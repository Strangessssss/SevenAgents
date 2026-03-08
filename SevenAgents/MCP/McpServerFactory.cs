using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SevenAgents.MCP;

/// <summary>
/// Factory for creating and invoking in-process MCP tools using the ModelContextProtocol SDK.
/// Annotate tool classes with <see cref="McpServerToolTypeAttribute"/> and methods with
/// <see cref="McpServerToolAttribute"/> — this factory handles the rest.
/// </summary>
public sealed class McpServerFactory
{
    private readonly Dictionary<string, (McpServerTool Tool, MethodInfo Method, object? Instance)> _tools =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All registered SDK tool descriptors (for metadata: name, description, schema).</summary>
    public IReadOnlyCollection<McpServerTool> Tools => _tools.Values.Select(t => t.Tool).ToList();

    /// <summary>
    /// Register all <see cref="McpServerToolAttribute"/>-annotated methods from <typeparamref name="T"/>.
    /// An instance is resolved from DI or created via Activator if not supplied.
    /// </summary>
    public McpServerFactory AddTools<T>() where T : class => AddTools(typeof(T));

    /// <summary>
    /// Register all <see cref="McpServerToolAttribute"/>-annotated methods from the given type.
    /// </summary>
    public McpServerFactory AddTools(Type toolType)
    {
        var instance = Activator.CreateInstance(toolType);

        foreach (var method in toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.GetCustomAttribute<McpServerToolAttribute>() is null) continue;
            var sdkTool = McpServerTool.Create(method, instance);
            _tools[sdkTool.ProtocolTool.Name] = (sdkTool, method, instance);
        }

        return this;
    }

    /// <summary>
    /// Scan <paramref name="assemblies"/> for types decorated with
    /// <see cref="McpServerToolTypeAttribute"/> and register their tools automatically.
    /// </summary>
    public McpServerFactory AddToolsFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is null) continue;
            AddTools(type);
        }

        return this;
    }

    /// <summary>Check whether a tool with the given name is registered.</summary>
    public bool HasTool(string name) => _tools.ContainsKey(name);

    /// <summary>
    /// Invoke a registered tool by name with the given arguments.
    /// Arguments are matched to method parameters by name (case-insensitive).
    /// Returns the string result.
    /// </summary>
    public async Task<string> CallToolAsync(
        string toolName,
        IDictionary<string, JsonElement>? arguments = null,
        CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var entry))
            throw new InvalidOperationException($"Tool '{toolName}' is not registered.");

        var (_, method, instance) = entry;
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            // Pass CancellationToken if the method accepts it
            if (param.ParameterType == typeof(CancellationToken))
            {
                args[i] = ct;
                continue;
            }

            if (arguments is not null &&
                arguments.TryGetValue(param.Name ?? string.Empty, out var element))
            {
                args[i] = element.Deserialize(param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
        }

        var returnValue = method.Invoke(instance, args);

        return returnValue switch
        {
            Task<string> t       => await t,
            Task<object?> t      => (await t)?.ToString() ?? string.Empty,
            Task t               => await t.ContinueWith(_ => string.Empty, ct),
            string s             => s,
            null                 => string.Empty,
            _                    => returnValue.ToString() ?? string.Empty
        };
    }
}

