#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPA.PrefabScanner.Editor
{
    [Serializable]
    public sealed class UpaPrefabDiagnostic
    {
        public string Code;
        public string Severity;
        public string Message;
        public string Path;

        public UpaPrefabDiagnostic(string code, string severity, string message, string path = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Path = path;
        }
    }

    [Serializable]
    public sealed class UpaPrefabComponentSnapshot
    {
        public string Id;
        public string TypeName;
        public bool MissingScript;
    }

    [Serializable]
    public sealed class UpaPrefabObjectSnapshot
    {
        public string Id;
        public string GlobalObjectId;
        public string Name;
        public string ParentId;
        public bool ActiveSelf;
        public int Layer;
        public string Tag;
        public string PrefabAssetPath;
        public bool IsNestedPrefabRoot;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public List<UpaPrefabComponentSnapshot> Components = new();
        public List<UpaPrefabObjectSnapshot> Children = new();
    }

    [Serializable]
    public sealed class UpaPrefabDependency
    {
        public string SourcePrefabPath;
        public string TargetPrefabPath;
        public string Kind;
    }

    [Serializable]
    public sealed class UpaPrefabSnapshot
    {
        public string PrefabPath;
        public string PrefabName;
        public string RootObjectId;
        public List<UpaPrefabObjectSnapshot> Roots = new();
        public List<UpaPrefabDependency> Dependencies = new();
        public List<UpaPrefabDiagnostic> Diagnostics = new();
    }

    public sealed class UpaPrefabScanner
    {
        public IReadOnlyList<UpaPrefabSnapshot> ScanProjectPrefabs()
        {
            var results = new List<UpaPrefabSnapshot>();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    results.Add(ScanPrefab(path));
            }

            return results
                .OrderBy(x => x.PrefabPath, StringComparer.Ordinal)
                .ToArray();
        }

        public UpaPrefabSnapshot ScanPrefab(string prefabPath)
        {
            var snapshot = new UpaPrefabSnapshot
            {
                PrefabPath = prefabPath.Replace('\\', '/'),
                PrefabName = Path.GetFileNameWithoutExtension(prefabPath)
            };

            var root = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                if (root == null)
                {
                    snapshot.Diagnostics.Add(new UpaPrefabDiagnostic(
                        "PREFAB-LOAD-001", "Error",
                        "Prefab contents could not be loaded.", prefabPath));
                    return snapshot;
                }

                snapshot.RootObjectId = StableId(
                    prefabPath,
                    GlobalObjectId.GetGlobalObjectIdSlow(root).ToString());

                snapshot.Roots.Add(SnapshotObject(
                    root, prefabPath, null, snapshot));

                return snapshot;
            }
            catch (Exception ex)
            {
                snapshot.Diagnostics.Add(new UpaPrefabDiagnostic(
                    "PREFAB-SCAN-001", "Error", ex.Message, prefabPath));
                return snapshot;
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static UpaPrefabObjectSnapshot SnapshotObject(
            GameObject go,
            string prefabPath,
            string parentId,
            UpaPrefabSnapshot snapshot)
        {
            var global = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            var result = new UpaPrefabObjectSnapshot
            {
                Id = StableId(prefabPath, global),
                GlobalObjectId = global,
                Name = go.name,
                ParentId = parentId,
                ActiveSelf = go.activeSelf,
                Layer = go.layer,
                Tag = SafeTag(go, snapshot),
                PrefabAssetPath = prefabPath,
                IsNestedPrefabRoot = false,
                LocalPosition = go.transform.localPosition,
                LocalRotation = go.transform.localRotation,
                LocalScale = go.transform.localScale
            };

            var nestedRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (nestedRoot != null && nestedRoot != go)
            {
                result.IsNestedPrefabRoot = true;

                var nestedAsset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(nestedAsset) &&
                    !string.Equals(nestedAsset, prefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.Dependencies.Add(new UpaPrefabDependency
                    {
                        SourcePrefabPath = prefabPath,
                        TargetPrefabPath = nestedAsset.Replace('\\', '/'),
                        Kind = "NestedPrefab"
                    });
                }
            }

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    result.Components.Add(new UpaPrefabComponentSnapshot
                    {
                        Id = StableId(prefabPath, global + ":missing"),
                        TypeName = "<Missing Script>",
                        MissingScript = true
                    });

                    snapshot.Diagnostics.Add(new UpaPrefabDiagnostic(
                        "PREFAB-MISSING-SCRIPT-001",
                        "Error",
                        $"Missing script detected on '{go.name}'.",
                        prefabPath));
                    continue;
                }

                var typeName = component.GetType().AssemblyQualifiedName ??
                               component.GetType().FullName ??
                               component.GetType().Name;

                result.Components.Add(new UpaPrefabComponentSnapshot
                {
                    Id = StableId(prefabPath, global + ":" + typeName),
                    TypeName = typeName,
                    MissingScript = false
                });
            }

            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                result.Children.Add(
                    SnapshotObject(child, prefabPath, result.Id, snapshot));
            }

            return result;
        }

        private static string SafeTag(GameObject go, UpaPrefabSnapshot snapshot)
        {
            try
            {
                return go.tag;
            }
            catch (UnityException)
            {
                snapshot.Diagnostics.Add(new UpaPrefabDiagnostic(
                    "PREFAB-TAG-001", "Warning",
                    $"Invalid or unavailable tag on '{go.name}'.",
                    snapshot.PrefabPath));
                return "<Undefined>";
            }
        }

        private static string StableId(string prefabPath, string objectKey)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                prefabPath.Replace('\\', '/') + "|" + objectKey);

            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(bytes))[..32].ToLowerInvariant();
        }
    }
}
#endif
