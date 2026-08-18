#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPA.AssetScanner.Editor.Tests
{
    public class UpaAssetScannerTests
    {
        private const string Folder = "Assets/UPA_TestAssetScanner";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(
                Path.Combine(Application.dataPath, "UPA_TestAssetScanner"));
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ScanAsset_DiscoversMaterialAndShader()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
                Assert.Ignore("Standard shader is unavailable in this Unity configuration.");

            var material = new Material(shader);
            const string path = Folder + "/Test.mat";

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var result = new UpaAssetScanner().ScanAsset(path);

            Assert.AreEqual(path, result.Path);
            Assert.AreEqual("Material", result.AssetKind);
            Assert.IsNotEmpty(result.ShaderName);

            // Scanner itself does not save or modify the material after creation.
            Object.DestroyImmediate(material);
        }
    }
}
#endif
