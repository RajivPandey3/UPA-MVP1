using UPA.Analysis;
using UPA.Core;

namespace UPA.Analysis.Tests;

public class CSharpParserAdversarialTests
{
    private static string CreateProject(string source)
    {
        var root = Directory.CreateTempSubdirectory();
        var assets = Path.Combine(root.FullName, "Assets", "Scripts");
        Directory.CreateDirectory(assets);
        File.WriteAllText(Path.Combine(assets, "Adversarial.cs"), source);
        return root.FullName;
    }

    private static CSharpScriptModel Scan(string source, out string root)
    {
        root = CreateProject(source);
        var result = new CSharpScanner().Scan(new ScanContext(root));
        return Assert.Single(result);
    }

    [Fact]
    public void Parser_HandlesBracesInsideCommentsAndStrings()
    {
        var model = Scan("""
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                // } fake brace
                string text = "{ not a real brace }";

                void Awake()
                {
                }
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Equal("TestBehaviour", type.Name);
            Assert.Contains("Awake", type.UnityLifecycleMethods);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesVerbatimString()
    {
        var model = Scan("""
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                string text = @"{ verbatim }";

                void Update()
                {
                }
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Equal("TestBehaviour", type.Name);
            Assert.Contains("Update", type.UnityLifecycleMethods);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesGenericAndNullableSerializedFields()
    {
        var model = Scan("""
            using System.Collections.Generic;
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                [SerializeField] private List<GameObject> objects;
                [SerializeField] private Transform? target;
                [SerializeField] private int[] values;
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Equal(3, type.SerializedFields.Count);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesMultipleTypes()
    {
        var model = Scan("""
            public class FirstType
            {
            }

            public struct SecondType
            {
            }

            public interface ThirdType
            {
            }

            public enum FourthType
            {
                A,
                B
            }
            """, out var root);

        try
        {
            Assert.Equal(4, model.Types.Count);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesNestedType()
    {
        var model = Scan("""
            public class OuterType
            {
                public class InnerType
                {
                }
            }
            """, out var root);

        try
        {
            Assert.Contains(model.Types, x => x.Name == "OuterType");
            Assert.Contains(model.Types, x => x.Name == "InnerType");
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_DoesNotTreatCommentAsRequireComponent()
    {
        var model = Scan("""
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                // [RequireComponent(typeof(Rigidbody))]
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Empty(type.RequiredComponents);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesMultipleRequiredComponents()
    {
        var model = Scan("""
            using UnityEngine;

            [RequireComponent(typeof(Rigidbody))]
            [RequireComponent(typeof(Collider))]
            public class TestBehaviour : MonoBehaviour
            {
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Contains("Rigidbody", type.RequiredComponents);
            Assert.Contains("Collider", type.RequiredComponents);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesMultilineSerializedDeclaration()
    {
        var model = Scan("""
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                [SerializeField]
                private
                Transform
                target;
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Single(type.SerializedFields);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_DoesNotTreatLocalVariableAsSerializedField()
    {
        var model = Scan("""
            using UnityEngine;

            public class TestBehaviour : MonoBehaviour
            {
                void Awake()
                {
                    int localValue = 10;
                }
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Empty(type.SerializedFields);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Parser_HandlesAttributesBeforeType()
    {
        var model = Scan("""
            using UnityEngine;

            [System.Serializable]
            public class SerializableData
            {
            }
            """, out var root);

        try
        {
            var type = Assert.Single(model.Types);
            Assert.Equal("SerializableData", type.Name);
        }
        finally { Directory.Delete(root, true); }
    }
}
