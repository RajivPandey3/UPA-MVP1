namespace UPA.Adapter;

public static class ParameterValidator
{
    public static IReadOnlyList<AdapterIssue> Validate(
        string operationId,
        IReadOnlyDictionary<string, object?> supplied,
        IReadOnlyList<(string Name, string Type, bool Required)> schema)
    {
        var issues = new List<AdapterIssue>();

        foreach (var parameter in schema)
        {
            supplied.TryGetValue(parameter.Name, out var value);

            if (parameter.Required && value is null)
            {
                issues.Add(new AdapterIssue(
                    "ADAPTER-PARAM-001",
                    "Error",
                    $"Required parameter '{parameter.Name}' is missing.",
                    operationId));
                continue;
            }

            if (value is null)
                continue;

            if (!TypeMatches(parameter.Type, value))
            {
                issues.Add(new AdapterIssue(
                    "ADAPTER-PARAM-002",
                    "Error",
                    $"Parameter '{parameter.Name}' does not match expected type '{parameter.Type}'.",
                    operationId));
            }
        }

        return issues;
    }

    private static bool TypeMatches(string expected, object value)
        => expected switch
        {
            "string" => value is string,
            "int" => value is int,
            "float" => value is float or double or decimal,
            "bool" => value is bool,
            "assetPath" => value is string s &&
                           s.StartsWith("Assets/", StringComparison.Ordinal),
            "globalObjectId" => value is string s &&
                                !string.IsNullOrWhiteSpace(s),
            "vector3" => value is System.Numerics.Vector3,
            "enum" => value is string,
            _ => false
        };
}
