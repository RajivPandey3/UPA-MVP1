#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace UPA.SettingsOps.Editor.Tests
{
    public class UpaSettingsOperationsTests
    {
        [Test]
        public void DryRunRejectsInvalidVSync()
        {
            var result = new UpaSettingsOperations().Execute(
                new UpaSettingsOperation
                {
                    OperationId = "vsync-invalid",
                    Kind = UpaSettingsOperationKind.SetVSyncCount,
                    IntValue = 9
                },
                true);

            Assert.False(result.Success);
            Assert.IsNotEmpty(result.Errors);
        }

        [Test]
        public void DryRunAcceptsValidPhysicsGravity()
        {
            var result = new UpaSettingsOperations().Execute(
                new UpaSettingsOperation
                {
                    OperationId = "gravity",
                    Kind = UpaSettingsOperationKind.SetPhysicsGravity,
                    Vector3Value = new Vector3(0, -9.81f, 0)
                },
                true);

            Assert.True(result.Success);
            Assert.IsNotEmpty(result.Audit);
        }

        [Test]
        public void DryRunRejectsMissingTextureImporter()
        {
            var result = new UpaSettingsOperations().Execute(
                new UpaSettingsOperation
                {
                    OperationId = "texture",
                    Kind = UpaSettingsOperationKind.SetTextureMaxSize,
                    AssetPath = "Assets/does-not-exist.png",
                    IntValue = 1024
                },
                true);

            Assert.False(result.Success);
            Assert.IsNotEmpty(result.Errors);
        }
    }
}
#endif
