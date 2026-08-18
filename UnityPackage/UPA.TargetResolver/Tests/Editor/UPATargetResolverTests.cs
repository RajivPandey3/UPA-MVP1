#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.TargetResolver.Editor.Tests
{
    public class UpaTargetResolverTests
    {
        [Test]
        public void GlobalObjectIdResolvesExactObject()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var go = new GameObject("Same");
            var id = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            var result = new UpaTargetResolver().Resolve(scene, id);

            Assert.True(result.Resolved);
            Assert.False(result.Ambiguous);
            Assert.AreEqual("Same", result.Name);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NameFallbackRejectsAmbiguousTarget()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var a = new GameObject("Same");
            var b = new GameObject("Same");

            var result = new UpaTargetResolver().Resolve(
                scene, null, "Same");

            Assert.False(result.Resolved);
            Assert.True(result.Ambiguous);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
#endif
