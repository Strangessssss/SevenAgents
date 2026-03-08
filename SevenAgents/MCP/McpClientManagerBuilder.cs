using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace SevenAgents.MCP;

public class McpClientManagerBuilder
{
    private readonly List<Type> _factoryTypes = [];
    private readonly List<Func<Task<McpClient>>> _clientFactories = [];
    private readonly List<Func<IConfiguration, Task<McpClient>>> _deferredConfigFactories = [];

    public McpClientManagerBuilder AddTools<T>() where T : class => AddTools(typeof(T));

    public McpClientManagerBuilder AddTools(Type type)
    {
        _factoryTypes.Add(type);
        return this;
    }

    public McpClientManagerBuilder AddStdioClient(
        string command,
        IEnumerable<string>? args = null,
        string? name = null,
        IDictionary<string, string>? env = null)
    {
        _clientFactories.Add(() => McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = command,
                Arguments = args?.ToList(),
                EnvironmentVariables = env?.ToDictionary(kv => kv.Key, kv => kv.Value)!
            }),
            new McpClientOptions { ClientInfo = new() { Name = name ?? command, Version = "1.0.0" } }
        ));
        return this;
    }

    public McpClientManagerBuilder AddHttpClient(string endpoint, string? name = null)
    {
        _clientFactories.Add(() => McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint)
            }),
            new McpClientOptions { ClientInfo = new() { Name = name ?? endpoint, Version = "1.0.0" } }
        ));
        return this;
    }

    /// <summary>Add an MCP server by name from the "McpServers" config section (config resolved eagerly).</summary>
    public McpClientManagerBuilder AddClientFromConfig(IConfiguration config, string serverName)
    {
        var section = config.GetSection("McpServers")
            .GetChildren()
            .FirstOrDefault(s => string.Equals(s["Name"], serverName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"MCP server '{serverName}' not found in configuration.");

        _clientFactories.Add(() => CreateClientFromSection(section));
        return this;
    }

    /// <summary>Add an MCP server by name, resolving config from IServiceProvider at build time.</summary>
    public McpClientManagerBuilder AddClientFromConfig(string serverName)
    {
        _deferredConfigFactories.Add(config =>
        {
            var section = config.GetSection("McpServers")
                .GetChildren()
                .FirstOrDefault(s => string.Equals(s["Name"], serverName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"MCP server '{serverName}' not found in configuration.");

            return CreateClientFromSection(section);
        });
        return this;
    }

    /// <summary>Add all MCP servers listed under "McpServers" in config (config resolved eagerly).</summary>
    public McpClientManagerBuilder AddAllClientsFromConfig(IConfiguration config)
    {
        foreach (var section in config.GetSection("McpServers").GetChildren())
        {
            var captured = section;
            _clientFactories.Add(() => CreateClientFromSection(captured));
        }
        return this;
    }

    /// <summary>Add all MCP servers listed under "McpServers", resolving config from IServiceProvider at build time.</summary>
    public McpClientManagerBuilder AddAllClientsFromConfig()
    {
        _deferredConfigFactories.Add(async config =>
        {
            McpClient? last = null;
            foreach (var section in config.GetSection("McpServers").GetChildren())
                last = await CreateClientFromSection(section);
            return last!;
        });
        return this;
    }

    /// <summary>Register as a keyed singleton in the DI container, resolving IConfiguration from the service provider.</summary>
    public void RegisterAs(IServiceCollection services, string key)
    {
        services.AddKeyedSingleton<McpClientManager>(key, (sp, _) =>
            BuildAsync(sp).GetAwaiter().GetResult());
    }

    public async Task<McpClientManager> BuildAsync(IServiceProvider? sp = null, CancellationToken ct = default)
    {
        var manager = new McpClientManager();

        if (_factoryTypes.Count > 0)
        {
            var factory = new McpServerFactory();
            foreach (var type in _factoryTypes)
                factory.AddTools(type);
            manager.AddFactory(factory);
        }

        foreach (var clientFactory in _clientFactories)
            manager.AddClient(await clientFactory());

        if (_deferredConfigFactories.Count > 0)
        {
            var config = sp?.GetRequiredService<IConfiguration>()
                ?? throw new InvalidOperationException("IServiceProvider is required to resolve deferred config clients.");

            foreach (var deferredFactory in _deferredConfigFactories)
                manager.AddClient(await deferredFactory(config));
        }

        return manager;
    }

    public Task<McpClientManager> BuildAsync(CancellationToken ct) => BuildAsync(null, ct);

    private static async Task<McpClient> CreateClientFromSection(IConfigurationSection section)
    {
        var name = section["Name"] ?? "unnamed";
        var transport = section["Transport"]?.ToLowerInvariant();

        return transport switch
        {
            "http" => await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(section["Endpoint"]
                        ?? throw new InvalidOperationException($"'{name}': missing Endpoint"))
                }),
                new McpClientOptions { ClientInfo = new() { Name = name, Version = "1.0.0" } }),

            "stdio" => await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Command = section["Command"]
                        ?? throw new InvalidOperationException($"'{name}': missing Command"),
                    Arguments = section.GetSection("Args").GetChildren().Select(a => a.Value!).ToList(),
                    EnvironmentVariables = section.GetSection("Env")
                        .GetChildren()
                        .ToDictionary(e => e.Key, e => e.Value ?? "")!
                }),
                new McpClientOptions { ClientInfo = new() { Name = name, Version = "1.0.0" } }),

            _ => throw new InvalidOperationException($"'{name}': unknown transport '{transport}'")
        };
    }
}
