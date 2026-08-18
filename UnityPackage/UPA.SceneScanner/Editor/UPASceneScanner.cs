#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.SceneScanner.Editor
{
    public enum UpaDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    [Serializable]
    public sealed class UpaDiagnostic
    {
        public string Code;
        public UpaDiagnosticSeverity Severity;
        public string Message;
        public string Path;

        public UpaDiagnostic(string code, UpaDiagnosticSeverity severity, string message, string path = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Path = path;
        }
    }

    [Serializable]
    public sealed class UpaComponentSnapshot
    {
        public string Id;
        public string TypeName;
        public bool MissingScript;
    }

    [Serializable]
    public sealed class UpaGameObjectSnapshot
    {
        public string Id;
        public string GlobalObjectId;
        public string Name;
        public string ParentId;
        public string ScenePath;
        public bool ActiveSelf;
        public bool ActiveInHierarchy;
        public int Layer;
        public string Tag;
        public string PrefabStatus;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public List<UpaComponentSnapshot> Components = new();
        public List<UpaGameObjectSnapshot> Children = new();
    }

    [Serializable]
    public sealed class UpaSceneSnapshot
    {
        public string ScenePath;
        public string SceneName;
        public bool IsLoaded;
        public List<UpaGameObjectSnapshot> Roots = new();
        public List<UpaDiagnostic> Diagnostics = new();
    }

    public sealed class UpaSceneScanner
    {
        public IReadOnlyList<UpaSceneSnapshot> ScanProjectScenes()
        {
            var results = new List<UpaSceneSnapshot>();

            foreach (var sceneGuid in AssetDatabase.FindAssets("t:Scene"))
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (string.IsNullOrEmpty(path))
                    continue;

                results.Add(ScanSceneAsset(path));
            }

            return results
                .OrderBy(x => x.ScenePath, StringComparer.Ordinal)
                .ToArray();
        }

        public UpaSceneSnapshot ScanOpenScene(Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Scene is invalid.", nameof(scene));

            return SnapshotLoadedScene(scene);
        }

        private UpaSceneSnapshot ScanSceneAsset(string scenePath)
        {
            var snapshot = new UpaSceneSnapshot
            {
                ScenePath = scenePath.Replace('\\', '/'),
                SceneName = Path.GetFileNameWithoutExtension(scenePath)
            };

            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var wasAlreadyLoaded = SceneManager.GetSceneByPath(scenePath).IsValid();

            try
            {
                Scene scene;

                if (wasAlreadyLoaded)
                {
                    scene = SceneManager.GetSceneByPath(scenePath);
                }
                else
                {
                    scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Additive);
                }

                snapshot = SnapshotLoadedScene(scene);

                if (!wasAlreadyLoaded)
                {
                    // Restore the exact editor scene setup without saving anything.
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
            catch (Exception ex)
            {
                snapshot.Diagnostics.Add(new UpaDiagnostic(
                    "SCENE-SCAN-001",
                    UpaDiagnosticSeverity.Error,
                    ex.Message,
                    scenePath));

                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
                catch
                {
                    // Do not hide the original diagnostic.
                }
            }

            return snapshot;
        }

        private static UpaSceneSnapshot SnapshotLoadedScene(Scene scene)
        {
            var snapshot = new UpaSceneSnapshot
            {
                ScenePath = scene.path.Replace('\\', '/'),
                SceneName = scene.name,
                IsLoaded = scene.isLoaded
            };

            foreach (var root in scene.GetRootGameObjects()
                         .OrderBy(x => x.transform.GetSiblingIndex())
                         .ThenBy(x => x.name, StringComparer.Ordinal))
            {
                snapshot.Roots.Add(SnapshotGameObject(root, scene.path, snapshot.Diagnostics));
            }

            return snapshot;
        }

        private static UpaGameObjectSnapshot SnapshotGameObject(
            GameObject go,
            string scenePath,
            List<UpaDiagnostic> diagnostics)
        {
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            var snapshot = new UpaGameObjectSnapshot
            {
                Id = StableId(scenePath, globalId),
                GlobalObjectId = globalId,
                Name = go.name,
                ScenePath = scenePath.Replace('\\', '/'),
                ActiveSelf = go.activeSelf,
                ActiveInHierarchy = go.activeInHierarchy,
                Layer = go.layer,
                Tag = SafeTag(go, diagnostics),
                PrefabStatus = PrefabUtility.GetPrefabInstanceStatus(go).ToString(),
                LocalPosition = go.transform.localPosition,
                LocalRotation = go.transform.localRotation,
                LocalScale = go.transform.localScale
            };

            var parent = go.transform.parent;
            if (parent != null)
            {
                var parentGlobal =
                    GlobalObjectId.GetGlobalObjectIdSlow(parent.gameObject).ToString();
                snapshot.ParentId = StableId(scenePath, parentGlobal);
            }

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    snapshot.Components.Add(new UpaComponentSnapshot
                    {
                        Id = StableId(scenePath, globalId + ":missing"),
                        TypeName = "<Missing Script>",
                        MissingScript = true
                    });

                    diagnostics.Add(new UpaDiagnostic(
                        "SCENE-MISSING-SCRIPT-001",
                        UpaDiagnosticSeverity.Error,
                        $"Missing script detected on GameObject '{go.name}'.",
                        scenePath));
                    continue;
                }

                var typeName = component.GetType().AssemblyQualifiedName ??
                               component.GetType().FullName ??
                               component.GetType().Name;

                snapshot.Components.Add(new UpaComponentSnapshot
                {
                    Id = StableId(scenePath, globalId + ":" + typeName),
                    TypeName = typeName,
                    MissingScript = false
                });
            }

            for (var i = 0; i < go.transform.childCount; i++)
            {
                snapshot.Children.Add(
                    SnapshotGameObject(go.transform.GetChild(i).gameObject,
                        scenePath, diagnostics));
            }

            return snapshot;
        }

        private static string SafeTag(GameObject go, List<UpaDiagnostic> diagnostics)
        {
            try
            {
                return go.tag;
            }
            catch (UnityException)
            {
                diagnostics.Add(new UpaDiagnostic(
                    "SCENE-TAG-001",
                    UpaDiagnosticSeverity.Warning,
                    $"Invalid or unavailable tag on '{go.name}'."));
                return "<Undefined>";
            }
        }

        private static string StableId(string scenePath, string objectKey)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                scenePath.Replace('\\', '/') + "|" + objectKey);

            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(bytes))[..32].ToLowerInvariant();
        }
    }
}
#endif
