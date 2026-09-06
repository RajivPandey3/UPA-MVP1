using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionDecisionPolicyTests
{
    [Fact]
    public void ClassifiesSafeDeterministicFindingAsAuto()
    {
        var finding = ActionDecisionPolicy.Classify("f1", "Create missing marker", EvidenceStatus.Confirmed, true, true);
        Assert.Equal(ActionMode.Auto, finding.Mode);
    }

    [Fact]
    public void NeverAutomatesUnknownOrStaleEvidence()
    {
        Assert.Equal(ActionMode.Unknown, ActionDecisionPolicy.Classify("f2", "Unknown", EvidenceStatus.Unknown, true, true).Mode);
        Assert.Equal(ActionMode.Unknown, ActionDecisionPolicy.Classify("f3", "Stale", EvidenceStatus.Stale, true, true).Mode);
    }

    [Fact]
    public void RoutesAmbiguousDecisionsToHuman()
    {
        Assert.Equal(ActionMode.Human, ActionDecisionPolicy.Classify("f4", "Choose art direction", EvidenceStatus.Confirmed, false, false).Mode);
        Assert.Equal(ActionMode.Assist, ActionDecisionPolicy.Classify("f5", "Preview config", EvidenceStatus.Confirmed, true, false).Mode);
    }
}
