namespace SevenAgents.Anthropic.Models;

public class AnthropicServiceConfig
{
    public string ModelId { get; init; } = string.Empty;

    public static AnthropicServiceConfig Haiku  => new() { ModelId = "claude-haiku-4-5" };
    public static AnthropicServiceConfig Sonnet => new() { ModelId = "claude-sonnet-4-5" };
    public static AnthropicServiceConfig Opus   => new() { ModelId = "claude-opus-4-5" };

    public static AnthropicServiceConfig Default => Sonnet;
}
