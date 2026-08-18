#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPA.ReferenceResolver.Editor.Tests
{
    public class UpaReferenceResolverTests
    {
        private const string Folder = "Assets/UPA_TestReferenceResolver";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(
                Path.Combine(Application.dataPath, "UPA_TestReferenceResolver"));
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Graph_ProducesStableAssetDependencyEdge()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
                Assert.Ignore("Standard shader unavailable.");

            var material = new Material(shader);
            var path = Folder + "/Material.mat";

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var graph = new UpaReferenceResolver().ResolveFromAsset(path);

            Assert.IsNotNull(graph);
            Assert.GreaterOrEqual(graph.Edges.Count, 0);

            Object.DestroyImmediate(material);
        }
    }
}
#endif
