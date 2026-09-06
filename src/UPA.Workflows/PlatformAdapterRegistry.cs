namespace UPA.Pipeline;

public sealed class PlatformAdapterRegistry
{
    private readonly Dictionary<string, IPlatformAdapter> adapters = new(StringComparer.OrdinalIgnoreCase);

    public PlatformAdapterRegistry(IEnumerable<IPlatformAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        foreach (var adapter in adapters) Register(adapter);
    }

    public IReadOnlyCollection<IPlatformAdapter> Adapters => adapters.Values.ToArray();

    public void Register(IPlatformAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        if (string.IsNullOrWhiteSpace(adapter.Id) || string.IsNullOrWhiteSpace(adapter.Version))
            throw new InvalidOperationException("An adapter must declare a non-empty id and version.");
        if (adapter.Capabilities is null || adapter.Capabilities.Count == 0 ||
            adapter.Capabilities.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("An adapter must declare at least one valid capability.");
        if (!adapters.TryAdd(adapter.Id, adapter))
            throw new InvalidOperationException($"Adapter '{adapter.Id}' is already registered.");
    }

    public IPlatformAdapter Resolve(string id, string capability)
    {
        if (!adapters.TryGetValue(id, out var adapter))
            throw new KeyNotFoundException($"No platform adapter is registered for '{id}'.");
        if (!adapter.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            throw new NotSupportedException($"Adapter '{id}' does not support capability '{capability}'.");
        return adapter;
    }
}
