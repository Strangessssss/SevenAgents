using System.Text.Json.Serialization;

namespace SevenAgents.Anthropic.Models;


public class StreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public AnthropicResponse? Message { get; set; }

    [JsonPropertyName("content_block")]
    public ContentBlock? ContentBlock { get; set; }

    [JsonPropertyName("delta")]
    public StreamDelta? Delta { get; set; }
}

public class StreamDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    // Tool use fields
    [JsonPropertyName("partial_json")]
    public string? PartialJson { get; set; }
}

