namespace UPA.Pipeline.Tests;

public sealed class OutputVerificationTests
{
    [Fact]
    public void RigidbodyOnDifferentObjectIsNotProof()
    {
        var root = Directory.CreateTempSubdirectory("upa-output-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "scene.unity"),
                "--- !u!1 &1\nGameObject:\n  m_Name: Player\n  m_Component:\n  - component: {fileID: 2}\n  - component: {fileID: 3}\n" +
                "--- !u!4 &2\nTransform:\n  m_GameObject: {fileID: 1}\n" +
                "--- !u!54 &3\nRigidbody:\n  m_GameObject: {fileID: 999}\n");
            Assert.Throws<InvalidOperationException>(() => OutputVerification.Verify(root.FullName,
                new[] { new OutputExpectation("scene.unity", "unity-player", "Player") }));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void UnknownVerificationKindCannotPass()
    {
        var root = Directory.CreateTempSubdirectory("upa-output-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "proof.txt"), "proof");
            Assert.Throws<InvalidOperationException>(() => OutputVerification.Verify(root.FullName,
                new[] { new OutputExpectation("proof.txt", "trust-me", "proof") }));
        }
        finally { root.Delete(true); }
    }
}
