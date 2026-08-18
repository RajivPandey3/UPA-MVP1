#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPA.AssetScanner.Editor
{
    [Serializable]
    public sealed class UpaAssetDiagnostic
    {
        public string Code;
        public string Severity;
        public string Message;
        public string Path;

        public UpaAssetDiagnostic(string code, string severity, string message, string path = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Path = path;
        }
    }

    [Serializable]
    public sealed class UpaAssetDependency
    {
        public string SourcePath;
        public string TargetPath;
        public bool Resolved;
    }

    [Serializable]
    public sealed class UpaAssetSnapshot
    {
        public string Id;
        public string Guid;
        public string Path;
        public string AssetKind;
        public string MainObjectType;
        public string ImporterType;
        public long FileSize;
        public int TextureWidth;
        public int TextureHeight;
        public string ShaderName;
        public List<UpaAssetDependency> Dependencies = new();
        public List<UpaAssetDiagnostic> Diagnostics = new();
    }

    public sealed class UpaAssetScanner
    {
        public IReadOnlyList<UpaAssetSnapshot> ScanProjectAssets()
        {
            var result = new List<UpaAssetSnapshot>();

            foreach (var guid in AssetDatabase.FindAssets("t:Object"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) ||
                    !path.StartsWith("Assets/", StringComparison.Ordinal))
                    continue;

                // Avoid scanning folders and meta files as logical assets.
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                result.Add(ScanAsset(path, guid));
            }

            return result
                .OrderBy(x => x.Path, StringComparer.Ordinal)
                .ToArray();
        }

        public UpaAssetSnapshot ScanAsset(string assetPath)
        {
            var normalized = assetPath.Replace('\\', '/');
            var guid = AssetDatabase.AssetPathToGUID(normalized);

            return ScanAsset(normalized, guid);
        }

        private UpaAssetSnapshot ScanAsset(string path, string guid)
        {
            var snapshot = new UpaAssetSnapshot
            {
                Id = StableId(guid.Length > 0 ? guid : path),
                Guid = guid,
                Path = path
            };

            try
            {
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                snapshot.MainObjectType = main != null
                    ? main.GetType().FullName
                    : "<None>";

                snapshot.AssetKind = Classify(main, path);

                var importer = AssetImporter.GetAtPath(path);
                snapshot.ImporterType = importer != null
                    ? importer.GetType().FullName
                    : null;

                var absolute = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    path);

                if (File.Exists(absolute))
                    snapshot.FileSize = new FileInfo(absolute).Length;

                if (main is Texture texture)
                {
                    snapshot.TextureWidth = texture.width;
                    snapshot.TextureHeight = texture.height;
                }

                if (main is Material material && material.shader != null)
                    snapshot.ShaderName = material.shader.name;

                ScanDependencies(snapshot);

                return snapshot;
            }
            catch (Exception ex)
            {
                snapshot.Diagnostics.Add(new UpaAssetDiagnostic(
                    "ASSET-SCAN-001",
                    "Error",
                    ex.Message,
                    path));
                return snapshot;
            }
        }

        private static void ScanDependencies(UpaAssetSnapshot snapshot)
        {
            var dependencies = AssetDatabase.GetDependencies(
                snapshot.Path,
                false);

            foreach (var dependency in dependencies
                .Where(x => !string.Equals(x, snapshot.Path, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.Ordinal))
            {
                var resolved = !string.IsNullOrEmpty(
                    AssetDatabase.AssetPathToGUID(dependency));

                snapshot.Dependencies.Add(new UpaAssetDependency
                {
                    SourcePath = snapshot.Path,
                    TargetPath = dependency.Replace('\\', '/'),
                    Resolved = resolved
                });

                if (!resolved)
                {
                    snapshot.Diagnostics.Add(new UpaAssetDiagnostic(
                        "ASSET-REF-001",
                        "Warning",
                        "Asset dependency could not be resolved.",
                        dependency));
                }
            }
        }

        private static string Classify(UnityEngine.Object main, string path)
        {
            if (main is Texture) return "Texture";
            if (main is Material) return "Material";
            if (main is Shader) return "Shader";
            if (main is Mesh) return "Mesh";
            if (main is AnimationClip) return "AnimationClip";
            if (main is AudioClip) return "AudioClip";
            if (main is GameObject) return "PrefabOrModel";
            if (main is ScriptableObject) return "ScriptableObject";

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".cs" => "CSharpScript",
                ".shader" => "ShaderSource",
                ".asmdef" => "AssemblyDefinition",
                ".unity" => "Scene",
                ".prefab" => "Prefab",
                ".mat" => "Material",
                ".controller" => "AnimatorController",
                ".anim" => "AnimationClip",
                _ => main != null ? main.GetType().Name : "Unknown"
            };
        }

        private static string StableId(string value)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToHexString(sha.ComputeHash(bytes))[..32].ToLowerInvariant();
        }
    }
}
#endif
