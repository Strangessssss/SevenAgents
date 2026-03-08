using SevenAgents.Anthropic.Models;
using SevenAgents.MCP;

namespace SevenAgents.Agents;

public class AgentConfig
{ public string AgentPrompt { get; set; } = string.Empty;
    
    public required McpClientManager McpClientManager { get; set; }
    public AnthropicServiceConfig AnthropicServiceConfig { get; set; } = AnthropicServiceConfig.Default;
    
    public List<string> AvailableTools { get; set; } = [];

    public static AgentConfig Create(
        McpClientManager mcpClientManager,
        List<string> prompts,
        AnthropicServiceConfig? aiServiceConfig = null)
    {
        return new AgentConfig
        {
            McpClientManager = mcpClientManager,
            AnthropicServiceConfig = aiServiceConfig ?? AnthropicServiceConfig.Default,
            AgentPrompt = string.Join("\n\n", prompts.Select(File.ReadAllText))
        };
    }
}