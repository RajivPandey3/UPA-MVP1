#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.SceneScanner.Editor.Tests
{
    public class UpaSceneScannerTests
    {
        [Test]
        public void Scanner_IsReadOnlyByConstruction()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var go = new GameObject("UPA_Test");
            go.AddComponent<BoxCollider>();
            SceneManager.MoveGameObjectToScene(go, scene);

            var before = go.transform.localPosition;

            var scanner = new UpaSceneScanner();
            var snapshot = scanner.ScanOpenScene(scene);

            Assert.AreEqual("UPA_Test", snapshot.Roots[0].Name);
            Assert.AreEqual(before, go.transform.localPosition);
            Assert.AreEqual(1, snapshot.Roots[0].Components.Count);

            Object.DestroyImmediate(go);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
#endif
