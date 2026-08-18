#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.UnityExecutor.Editor.Tests
{
    public class UpaUnityExecutorTests
    {
        [Test]
        public void DryRunDoesNotCreateObject()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var executor = new UpaUnityExecutor();

            var result = executor.Execute(
                "plan-test",
                scene.path,
                null,
                new[]
                {
                    new UpaUnityMutation
                    {
                        OperationId = "create-1",
                        Kind = UpaUnityMutationKind.CreateGameObject,
                        TargetObjectName = "UPA_DryRunObject"
                    }
                },
                true);

            Assert.True(result.Success);
            Assert.IsNull(GameObject.Find("UPA_DryRunObject"));
        }

        [Test]
        public void RealMutationRequiresApproval()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var executor = new UpaUnityExecutor();

            var result = executor.Execute(
                "plan-test",
                scene.path,
                null,
                new[]
                {
                    new UpaUnityMutation
                    {
                        OperationId = "create-1",
                        Kind = UpaUnityMutationKind.CreateGameObject,
                        TargetObjectName = "UPA_ShouldNotExist"
                    }
                },
                false);

            Assert.False(result.Success);
            Assert.IsNull(GameObject.Find("UPA_ShouldNotExist"));
        }
    }
}
#endif
