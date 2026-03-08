using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SevenAgents.MCP;

public class McpClientManager
{
    public static McpClientManagerBuilder Builder() => new();

    private readonly List<McpClient> _clients = [];
    private readonly List<McpServerFactory> _factories = [];
    private readonly Dictionary<string, McpClient> _toolClientMap = new();
    private readonly Dictionary<string, McpServerFactory> _toolFactoryMap = new();
    private List<McpClientTool>? _cachedMcpTools;
    private List<McpAnthropicTool>? _cachedAllAnthropicTools;

    public int ClientCount => _clients.Count;
    public int FactoryCount => _factories.Count;
    
    public void AddClient(McpClient client)
    {
        _clients.Add(client);
        InvalidateCache();
    }

    /// <summary>Register an in-process tool factory (replaces CustomMcpServer).</summary>
    public void AddFactory(McpServerFactory factory)
    {
        _factories.Add(factory);
        InvalidateCache();
    }

    /// <summary>Lists all tools from real MCP servers.</summary>
    public async Task<IReadOnlyList<McpClientTool>> ListAllToolsAsync(CancellationToken ct = default)
    {
        if (_cachedMcpTools is not null) return _cachedMcpTools;

        var allTools = new List<McpClientTool>();
        _toolClientMap.Clear();

        foreach (var client in _clients)
        foreach (var tool in await client.ListToolsAsync(cancellationToken: ct))
        {
            _toolClientMap[tool.Name] = client;
            allTools.Add(tool);
        }

        _cachedMcpTools = allTools;
        return allTools;
    }

    /// <summary>
    /// Lists all tools (MCP client tools + in-process factory tools) in the unified Anthropic format.
    /// </summary>
    public async Task<IReadOnlyList<McpAnthropicTool>> ListAllAnthropicToolsAsync(CancellationToken ct = default)
    {
        if (_cachedAllAnthropicTools is not null) return _cachedAllAnthropicTools;

        var mcpTools = await ListAllToolsAsync(ct);
        var anthropicTools = McpToolConverter.ToAnthropicTools(mcpTools);

        _toolFactoryMap.Clear();
        foreach (var factory in _factories)
        foreach (var tool in factory.Tools)
        {
            _toolFactoryMap[tool.ProtocolTool.Name] = factory;
            anthropicTools.Add(McpToolConverter.ToAnthropicTool(tool));
        }

        _cachedAllAnthropicTools = anthropicTools;
        return anthropicTools;
    }

    /// <summary>
    /// Call a tool by name. Resolves to either an in-process factory or a real MCP server.
    /// </summary>
    public async Task<string> CallToolAsync(
        string toolName,
        IDictionary<string, JsonElement>? arguments = null,
        CancellationToken ct = default)
    {
        if (_cachedAllAnthropicTools is null)
            await ListAllAnthropicToolsAsync(ct);
        
        if (_toolFactoryMap.TryGetValue(toolName, out var factory))
            return await factory.CallToolAsync(toolName, arguments, ct);
        
        if (_toolClientMap.TryGetValue(toolName, out var client))
        {
            // Convert JsonElement dict → object? dict for MCP client
            IReadOnlyDictionary<string, object?>? objectArgs = arguments?
                .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            var result = await client.CallToolAsync(toolName, objectArgs, cancellationToken: ct);
            return string.Join("\n", result.Content.Select(c => c.ToString() ?? ""));
        }

        throw new InvalidOperationException($"Tool '{toolName}' not found.");
    }

    /// <summary>Call a tool on a real MCP client and return the raw result.</summary>
    public async Task<CallToolResult> CallMcpToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken ct = default)
    {
        if (_cachedMcpTools is null)
            await ListAllToolsAsync(ct);

        if (!_toolClientMap.TryGetValue(toolName, out var client))
            throw new InvalidOperationException($"Tool '{toolName}' not found on any MCP server.");

        return await client.CallToolAsync(toolName, arguments, cancellationToken: ct);
    }

    private void InvalidateCache()
    {
        _cachedMcpTools = null;
        _cachedAllAnthropicTools = null;
    }
}
