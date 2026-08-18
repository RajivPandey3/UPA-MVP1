#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPA.ReferenceResolver.Editor
{
    public enum UpaReferenceKind
    {
        AssetDependency,
        SerializedObjectReference,
        SceneObjectReference,
        ScriptReference,
        Unknown
    }

    [Serializable]
    public sealed class UpaReferenceEdge
    {
        public string Source;
        public string Target;
        public UpaReferenceKind Kind;
        public bool Resolved;
        public int Depth;
    }

    [Serializable]
    public sealed class UpaReverseReference
    {
        public string Target;
        public List<string> Sources = new();
    }

    [Serializable]
    public sealed class UpaReferenceDiagnostic
    {
        public string Code;
        public string Severity;
        public string Message;
        public string Source;

        public UpaReferenceDiagnostic(
            string code, string severity, string message, string source = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Source = source;
        }
    }

    [Serializable]
    public sealed class UpaReferenceGraph
    {
        public List<UpaReferenceEdge> Edges = new();
        public List<UpaReverseReference> ReverseReferences = new();
        public List<UpaReferenceDiagnostic> Diagnostics = new();
    }

    public sealed class UpaReferenceResolver
    {
        public UpaReferenceGraph BuildAssetGraph()
        {
            var graph = new UpaReferenceGraph();

            foreach (var guid in AssetDatabase.FindAssets("t:Object")
                .OrderBy(x => x, StringComparer.Ordinal))
            {
                var source = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(source) ||
                    AssetDatabase.IsValidFolder(source))
                    continue;

                AddDirectDependencies(source, graph);
            }

            BuildReverseIndex(graph);
            return graph;
        }

        public UpaReferenceGraph ResolveFromAsset(string sourcePath)
        {
            var graph = new UpaReferenceGraph();
            AddDirectDependencies(sourcePath.Replace('\\', '/'), graph);
            BuildReverseIndex(graph);
            return graph;
        }

        private static void AddDirectDependencies(
            string source,
            UpaReferenceGraph graph)
        {
            string[] dependencies;

            try
            {
                dependencies = AssetDatabase.GetDependencies(source, false);
            }
            catch (Exception ex)
            {
                graph.Diagnostics.Add(new UpaReferenceDiagnostic(
                    "REF-DEP-001", "Error", ex.Message, source));
                return;
            }

            foreach (var target in dependencies
                .Select(x => x.Replace('\\', '/'))
                .Where(x => !string.Equals(x, source, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal))
            {
                var resolved = !string.IsNullOrEmpty(
                    AssetDatabase.AssetPathToGUID(target));

                graph.Edges.Add(new UpaReferenceEdge
                {
                    Source = source,
                    Target = target,
                    Kind = UpaReferenceKind.AssetDependency,
                    Resolved = resolved,
                    Depth = 1
                });

                if (!resolved)
                {
                    graph.Diagnostics.Add(new UpaReferenceDiagnostic(
                        "REF-MISSING-001",
                        "Warning",
                        "Dependency path could not be resolved to an asset GUID.",
                        source));
                }
            }
        }

        private static void BuildReverseIndex(UpaReferenceGraph graph)
        {
            graph.ReverseReferences.Clear();

            foreach (var group in graph.Edges
                .Where(x => x.Resolved)
                .GroupBy(x => x.Target, StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                graph.ReverseReferences.Add(new UpaReverseReference
                {
                    Target = group.Key,
                    Sources = group
                        .Select(x => x.Source)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList()
                });
            }
        }
    }
}
#endif
