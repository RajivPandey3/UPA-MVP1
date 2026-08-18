#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPA.PrefabScanner.Editor.Tests
{
    public class UpaPrefabScannerTests
    {
        private const string Folder = "Assets/UPA_TestPrefabScanner";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(Path.Combine(
                Application.dataPath, "UPA_TestPrefabScanner"));
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ScanPrefab_DiscoversHierarchyAndComponents()
        {
            var go = new GameObject("Root");
            go.AddComponent<BoxCollider>();

            var child = new GameObject("Child");
            child.transform.SetParent(go.transform);
            child.AddComponent<Rigidbody>();

            var path = Folder + "/Test.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);

            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();

            var result = new UpaPrefabScanner().ScanPrefab(path);

            Assert.AreEqual("Test", result.PrefabName);
            Assert.AreEqual(1, result.Roots.Count);
            Assert.AreEqual("Root", result.Roots[0].Name);
            Assert.AreEqual(1, result.Roots[0].Children.Count);
            Assert.AreEqual("Child", result.Roots[0].Children[0].Name);
            Assert.IsFalse(result.Roots[0].Components[0].MissingScript);
        }
    }
}
#endif
