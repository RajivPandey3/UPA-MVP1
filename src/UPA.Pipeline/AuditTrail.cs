namespace UPA.Pipeline;

public sealed class AuditTrail
{
    private readonly List<PipelineEvent> _events = new();

    public void Append(PipelineEvent evt)
        => _events.Add(evt);

    public IReadOnlyList<PipelineEvent> Snapshot()
        => _events.ToArray();

    public string ToText()
        => string.Join(
            Environment.NewLine,
            _events.Select(x =>
                $"{x.TimestampUtc:O} | {x.Stage} | {x.Code} | {x.Message}"));
}
