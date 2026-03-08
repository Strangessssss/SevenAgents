using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

namespace SevenAgents.MCP;

public record McpAnthropicTool(string Name, string Description, JsonElement InputSchema);

public static class McpToolConverter
{
    private static readonly JsonElement EmptyObjectSchema =
        JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement.Clone();

    /// <summary>Convert MCP client tools to the Anthropic tool format.</summary>
    public static List<McpAnthropicTool> ToAnthropicTools(IEnumerable<McpClientTool> mcpTools) =>
        mcpTools.Select(t => new McpAnthropicTool(t.Name, t.Description, t.JsonSchema)).ToList();

    /// <summary>Convert a single SDK McpServerTool to the Anthropic tool format.</summary>
    public static McpAnthropicTool ToAnthropicTool(McpServerTool tool) =>
        new(tool.ProtocolTool.Name,
            tool.ProtocolTool.Description ?? string.Empty,
            SanitizeSchema(tool.ProtocolTool.InputSchema));

    /// <summary>Convert SDK McpServerTools to the Anthropic tool format.</summary>
    public static List<McpAnthropicTool> ToAnthropicTools(IEnumerable<McpServerTool> tools) =>
        tools.Select(ToAnthropicTool).ToList();

    /// <summary>
    /// Ensures the schema is a valid Anthropic input_schema.
    /// Anthropic requires {"type":"object", "properties": {...}} and
    /// rejects extra keys like "$schema".
    /// </summary>
    private static JsonElement SanitizeSchema(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
            return EmptyObjectSchema;

        // Build a clean schema keeping only the keys Anthropic accepts
        var clean = new Dictionary<string, object?> { ["type"] = "object" };

        if (schema.TryGetProperty("properties", out var props) &&
            props.ValueKind == JsonValueKind.Object)
        {
            clean["properties"] = props;
        }
        else
        {
            clean["properties"] = new Dictionary<string, object>();
        }

        if (schema.TryGetProperty("required", out var req) &&
            req.ValueKind == JsonValueKind.Array)
        {
            clean["required"] = req;
        }

        if (schema.TryGetProperty("additionalProperties", out var ap))
        {
            clean["additionalProperties"] = ap;
        }

        var json = JsonSerializer.Serialize(clean);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
