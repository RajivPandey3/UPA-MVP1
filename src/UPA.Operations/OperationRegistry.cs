namespace UPA.Operations;

public sealed class OperationRegistry
{
    private readonly Dictionary<string, OperationDefinition> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _aliasToId =
        new(StringComparer.OrdinalIgnoreCase);

    public OperationRegistry Register(OperationDefinition definition)
    {
        if (_byId.ContainsKey(definition.Id))
            throw new InvalidOperationException(
                $"Operation already registered: {definition.Id}");

        _byId.Add(definition.Id, definition);

        foreach (var alias in definition.Aliases)
        {
            if (_aliasToId.ContainsKey(alias))
                throw new InvalidOperationException(
                    $"Alias collision: {alias}");

            _aliasToId.Add(alias, definition.Id);
        }

        return this;
    }

    public OperationDefinition Get(string id)
    {
        if (!_byId.TryGetValue(id, out var definition))
            throw new KeyNotFoundException(
                $"Unknown operation: {id}");

        return definition;
    }

    public bool TryResolveAlias(
        string phrase,
        out OperationDefinition? definition)
    {
        definition = null;

        if (!_aliasToId.TryGetValue(Normalize(phrase), out var id))
            return false;

        definition = _byId[id];
        return true;
    }

    public IReadOnlyList<OperationDefinition> All()
        => _byId.Values
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();

    private static string Normalize(string value)
        => value.Trim()
            .ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ");
}
