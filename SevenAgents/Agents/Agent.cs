using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenAgents.Anthropic;
using SevenAgents.MCP;
using SevenAgents.Messages;

namespace SevenAgents.Agents;

public sealed class Agent
{ 
    public string AgentPrompt { get; }
    private AnthropicService AiService { get; }

    private List<Message> ConversationHistory { get; } = [];

    public async Task<string> SayAsync(string prompt, Func<string, Task>? onToken = null)
    {
        ConversationHistory.Add(new Message(prompt, Role.User));

        var response = await AiService.StreamProcessMessages(
            conversationHistory: ConversationHistory,
            systemMessage: AgentPrompt,
            onToken: onToken);

        ConversationHistory.Add(new Message(response, Role.Assistant));
        return response;
    }

    public Agent(AgentConfig config, IServiceProvider sp)
    {
        AgentPrompt = config.AgentPrompt;

        var apiKey = sp.GetRequiredService<IConfiguration>()["ApiKeys:Anthropic"]
                     ?? throw new InvalidOperationException("ApiKeys:Anthropic not configured.");

        AiService = new AnthropicService(apiKey, config.McpClientManager, config.AnthropicServiceConfig.ModelId);
    }
}