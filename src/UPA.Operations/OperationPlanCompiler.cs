namespace UPA.Operations;

public sealed record CompiledOperation(
    string OperationId,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<OperationParameter> RequiredParameters,
    OperationRisk Risk,
    double Confidence,
    string Preview);

public sealed record CompiledOperationPlan(
    IReadOnlyList<CompiledOperation> Operations,
    IReadOnlyList<string> Warnings);

public sealed class OperationPlanCompiler
{
    public CompiledOperationPlan Compile(
        IReadOnlyList<OperationMatch> matches)
    {
        var warnings = new List<string>();
        var definitions = matches
            .Select(x => x.Definition)
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id, StringComparer.Ordinal);

        var warned = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in definitions.Values)
        {
            AddMissingDependencies(definition, definitions, warnings, warned);
        }

        var operations = definitions.Values
            .OrderBy(x => x.DependsOn.Count)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Select(d =>
            {
                var match = matches.First(x => x.Definition.Id == d.Id);

                return new CompiledOperation(
                    d.Id,
                    d.DependsOn,
                    d.Parameters.Where(x => x.Required).ToArray(),
                    d.Risk,
                    match.Confidence,
                    d.PreviewTemplate);
            })
            .ToArray();

        return new CompiledOperationPlan(operations, warnings);
    }

    private static void AddMissingDependencies(
        OperationDefinition definition,
        IReadOnlyDictionary<string, OperationDefinition> definitions,
        ICollection<string> warnings,
        ISet<string> warned)
    {
        foreach (var dependencyId in definition.DependsOn)
        {
            if (!definitions.TryGetValue(dependencyId, out var dependency))
            {
                var warning =
                    $"Operation '{definition.Id}' depends on '{dependencyId}', " +
                    "which is not present in the matched intent.";

                if (warned.Add(warning))
                    warnings.Add(warning);

                continue;
            }

            AddMissingDependencies(dependency, definitions, warnings, warned);
        }
    }

}
