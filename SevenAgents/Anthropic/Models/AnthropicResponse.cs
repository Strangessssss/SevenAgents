using System.Text.Json;
using System.Text.Json.Serialization;

namespace SevenAgents.Anthropic.Models;

public class AnthropicResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = [];

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; } = new();
}

public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    // Tool use fields (present when type == "tool_use")
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    public JsonElement? Input { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }

    [JsonPropertyName("cache_creation")]
    public CacheCreationInfo? CacheCreation { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }

    [JsonPropertyName("inference_geo")]
    public string? InferenceGeo { get; set; }
}

public class CacheCreationInfo
{
    [JsonPropertyName("ephemeral_5m_input_tokens")]
    public int Ephemeral5MInputTokens { get; set; }

    [JsonPropertyName("ephemeral_1h_input_tokens")]
    public int Ephemeral1HInputTokens { get; set; }
}

