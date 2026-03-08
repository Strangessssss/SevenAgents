using System.Text;
using System.Text.Json;
using SevenAgents.Anthropic.Models;
using SevenAgents.MCP;
using SevenAgents.Messages;

namespace SevenAgents.Anthropic;

public class AnthropicService(string apiKey, McpClientManager mcpManager, string model = "claude-sonnet-4-5")
{
    private const int MaxIterations = 10;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public async Task<string> StreamProcessMessages(
        List<Message>? conversationHistory = null,
        int maxTokens = 4096,
        string systemMessage = "",
        Func<string, Task>? onToken = null)
    {
        conversationHistory ??= [];

        var tools = (await mcpManager.ListAllAnthropicToolsAsync()).ToList();

        var messages = conversationHistory
            .Select(m => new { role = m.Sender, content = m.Content })
            .Cast<object>()
            .ToList();

        for (var i = 0; i < MaxIterations; i++)
        {
            var result = await StreamCallAnthropic(messages, tools, maxTokens, systemMessage, onToken);

            if (result.StopReason == "end_turn")
                return result.FullText;

            if (result.StopReason == "tool_use")
            {
                messages.Add(new
                {
                    role = "assistant",
                    content = result.ContentBlocks.Select<StreamContentBlock, object>(b => b.Type switch
                    {
                        "tool_use" => new { type = "tool_use", id = b.Id, name = b.Name, input = b.InputJson },
                        _          => new { type = "text", text = b.Text }
                    }).ToList()
                });
                
                var toolResults = new List<object>();
                foreach (var b in result.ContentBlocks.Where(c => c.Type == "tool_use"))
                {
                    var args = Deserialize(b.InputJson);
                    var toolResult = await mcpManager.CallToolAsync(b.Name!, args);

                    toolResults.Add(new { type = "tool_result", tool_use_id = b.Id, content = toolResult });
                }

                messages.Add(new { role = "user", content = toolResults });
                continue;
            }

            throw new InvalidOperationException($"Unexpected stop_reason: {result.StopReason}");
        }

        throw new InvalidOperationException($"Tool loop exceeded {MaxIterations} iterations.");
    }

    #region Streaming internals

    private record StreamResult(string StopReason, string FullText, List<StreamContentBlock> ContentBlocks);

    private class StreamContentBlock
    {
        public string Type { get; set; } = "";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string Text { get; set; } = "";
        public string InputJsonRaw { get; set; } = "";

        private static readonly JsonElement EmptyObject =
            JsonDocument.Parse("{}").RootElement.Clone();

        public JsonElement InputJson =>
            string.IsNullOrEmpty(InputJsonRaw) ? EmptyObject : JsonDocument.Parse(InputJsonRaw).RootElement;
    }

    private async Task<StreamResult> StreamCallAnthropic(
        List<object> messages,
        List<McpAnthropicTool> tools,
        int maxTokens,
        string system,
        Func<string, Task>? onToken)
    {
        object requestBody = tools.Count > 0
            ? new
            {
                model,
                max_tokens = maxTokens,
                system,
                messages,
                stream = true,
                tools = tools.Select(t => new { name = t.Name, description = t.Description, input_schema = t.InputSchema })
            }
            : new
            {
                model,
                max_tokens = maxTokens,
                system,
                messages,
                stream = true
            };

        var body = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var resp = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Anthropic API error {(int)resp.StatusCode}: {errorBody}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var blocks = new List<StreamContentBlock>();
        var fullText = new StringBuilder();
        string? stopReason = null;

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            if (!line.StartsWith("data: ")) continue;
            var json = line["data: ".Length..];
            if (json == "[DONE]") break;

            StreamEvent? evt;
            try { evt = JsonSerializer.Deserialize<StreamEvent>(json); }
            catch { continue; }
            if (evt is null) continue;

            switch (evt.Type)
            {
                case "content_block_start":
                {
                    var idx = evt.Index;
                    var block = new StreamContentBlock
                    {
                        Type = evt.ContentBlock?.Type ?? "text",
                        Id = evt.ContentBlock?.Id,
                        Name = evt.ContentBlock?.Name
                    };
                    // Ensure the list is big enough
                    while (blocks.Count <= idx) blocks.Add(new StreamContentBlock());
                    blocks[idx] = block;
                    break;
                }

                case "content_block_delta":
                {
                    var idx = evt.Index;
                    if (idx < blocks.Count && evt.Delta is not null)
                    {
                        var b = blocks[idx];
                        if (evt.Delta.Type == "text_delta" && evt.Delta.Text is not null)
                        {
                            b.Text += evt.Delta.Text;
                            fullText.Append(evt.Delta.Text);
                            if (onToken is not null)
                                await onToken(evt.Delta.Text);
                        }
                        else if (evt.Delta.Type == "input_json_delta" && evt.Delta.PartialJson is not null)
                        {
                            b.InputJsonRaw += evt.Delta.PartialJson;
                        }
                    }
                    break;
                }

                case "message_delta":
                    if (evt.Delta?.StopReason is not null)
                        stopReason = evt.Delta.StopReason;
                    break;
            }
        }

        return new StreamResult(
            stopReason ?? "end_turn",
            fullText.ToString(),
            blocks);
    }

    #endregion

    private static Dictionary<string, JsonElement> Deserialize(JsonElement? input) =>
        input is null || input.Value.ValueKind == JsonValueKind.Null
            ? new()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(input.Value.GetRawText()) ?? new();
}
