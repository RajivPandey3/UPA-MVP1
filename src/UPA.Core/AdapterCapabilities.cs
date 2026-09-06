namespace UPA.Core;

public sealed record AdapterCapability(string AdapterId, string CapabilityId, CompatibilityStatus Status, string Evidence);

public sealed class AdapterCapabilityRegistry
{
    private readonly Dictionary<(string Adapter, string Capability), AdapterCapability> capabilities = new();

    public void Register(AdapterCapability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (string.IsNullOrWhiteSpace(capability.AdapterId) || string.IsNullOrWhiteSpace(capability.CapabilityId))
            throw new ArgumentException("Adapter and capability identifiers are required.");
        if (capability.Status == CompatibilityStatus.Verified && string.IsNullOrWhiteSpace(capability.Evidence))
            throw new ArgumentException("Verified capability requires evidence.");
        capabilities[(capability.AdapterId, capability.CapabilityId)] = capability;
    }

    public AdapterCapability Resolve(string adapterId, string capabilityId) =>
        capabilities.TryGetValue((adapterId, capabilityId), out var capability)
            ? capability
            : new AdapterCapability(adapterId, capabilityId, CompatibilityStatus.Unknown, "No capability evidence.");

    public void EnsureExecutable(string adapterId, string capabilityId)
    {
        var capability = Resolve(adapterId, capabilityId);
        if (capability.Status != CompatibilityStatus.Verified)
            throw new NotSupportedException($"Capability '{capabilityId}' is not verified for adapter '{adapterId}'.");
    }
}
