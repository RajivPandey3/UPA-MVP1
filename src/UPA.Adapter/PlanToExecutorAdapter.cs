using UPA.Operations;

namespace UPA.Adapter;

public sealed class PlanToExecutorAdapter
{
    private readonly OperationBindingCatalog _catalog;

    public PlanToExecutorAdapter(OperationBindingCatalog catalog)
    {
        _catalog = catalog;
    }

    public BoundExecutionPlan Bind(
        string planId,
        IReadOnlyList<CompiledOperationWithArguments> operations)
    {
        var issues = new List<AdapterIssue>();
        var bound = new List<BoundOperation>();

        foreach (var input in operations)
        {
            OperationBinding binding;

            try
            {
                binding = _catalog.Get(input.OperationId);
            }
            catch (KeyNotFoundException ex)
            {
                issues.Add(new AdapterIssue(
                    "ADAPTER-BIND-001",
                    "Error",
                    ex.Message,
                    input.OperationId));
                continue;
            }

            issues.AddRange(ParameterValidator.Validate(
                input.OperationId,
                input.Arguments,
                binding.Parameters));

            var preconditions = binding.Preconditions(
                new OperationArguments(input.Arguments));

            bound.Add(new BoundOperation(
                input.OperationId,
                binding.Executor,
                new OperationArguments(input.Arguments),
                preconditions,
                input.DependsOn,
                input.Confidence,
                input.RequiresApproval));
        }

        var batches = BuildBatches(bound, issues);

        var hasBlockingError = issues.Any(x =>
            string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));

        return new BoundExecutionPlan(
            planId,
            batches,
            issues,
            ReadyForPreview: !hasBlockingError,
            ReadyForExecution: false);
    }

    private static IReadOnlyList<ExecutionBatch> BuildBatches(
        IReadOnlyList<BoundOperation> operations,
        List<AdapterIssue> issues)
    {
        var result = new List<ExecutionBatch>();
        var current = new List<BoundOperation>();
        AdapterExecutor? family = null;

        foreach (var operation in operations)
        {
            if (family != null && operation.Executor != family)
            {
                result.Add(new ExecutionBatch(
                    "batch-" + result.Count.ToString("D3"),
                    current.ToArray(),
                    current.Any(x => x.RequiresApproval)));
                current = new List<BoundOperation>();
            }

            family = operation.Executor;
            current.Add(operation);
        }

        if (current.Count > 0)
        {
            result.Add(new ExecutionBatch(
                "batch-" + result.Count.ToString("D3"),
                current.ToArray(),
                current.Any(x => x.RequiresApproval)));
        }

        return result;
    }
}

public sealed record CompiledOperationWithArguments(
    string OperationId,
    IReadOnlyDictionary<string, object?> Arguments,
    IReadOnlyList<string> DependsOn,
    double Confidence,
    bool RequiresApproval);
