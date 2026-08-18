#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.AdvancedOps.Editor.Tests
{
    public class UpaAdvancedOperationsTests
    {
        [Test]
        public void DryRunRigidbodyDoesNotCreateComponent()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var go = new GameObject("Player");

            var result = new UpaAdvancedOperations().Execute(
                scene,
                new UpaAdvancedOperation
                {
                    OperationId = "rb-1",
                    Kind = UpaAdvancedOperationKind.ConfigureRigidbody,
                    TargetGlobalObjectId =
                        GlobalObjectId.GetGlobalObjectIdSlow(go).ToString()
                },
                true);

            Assert.True(result.Success);
            Assert.IsNull(go.GetComponent<Rigidbody>());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void DuplicateNameIsRejected()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var a = new GameObject("Player");
            var b = new GameObject("Player");

            var result = new UpaAdvancedOperations().Execute(
                scene,
                new UpaAdvancedOperation
                {
                    OperationId = "tag-1",
                    Kind = UpaAdvancedOperationKind.SetTag,
                    FallbackTargetName = "Player",
                    StringValue = "Untagged"
                },
                true);

            Assert.False(result.Success);
            Assert.IsNotEmpty(result.Errors);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
#endif
