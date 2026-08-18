#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.TargetResolver.Editor
{
    [Serializable]
    public sealed class UpaTargetResult
    {
        public bool Resolved;
        public bool Ambiguous;
        public string GlobalObjectId;
        public string Name;
        public string ScenePath;
        public string Error;
    }

    public sealed class UpaTargetResolver
    {
        public UpaTargetResult Resolve(
            Scene scene,
            string globalObjectId,
            string fallbackName = null)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return Error("Target scene is invalid or not loaded.");

            if (!string.IsNullOrWhiteSpace(globalObjectId))
            {
                var target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(
                    GlobalObjectId.TryParse(globalObjectId, out var parsed)
                        ? parsed
                        : default);

                if (target is GameObject exact &&
                    exact.scene == scene)
                {
                    return new UpaTargetResult
                    {
                        Resolved = true,
                        GlobalObjectId = globalObjectId,
                        Name = exact.name,
                        ScenePath = scene.path
                    };
                }
            }

            if (string.IsNullOrWhiteSpace(fallbackName))
                return Error("No stable GlobalObjectId was supplied and no fallback name exists.");

            var matches = new List<GameObject>();

            foreach (var root in scene.GetRootGameObjects())
                CollectByName(root.transform, fallbackName, matches);

            if (matches.Count == 1)
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(matches[0]).ToString();
                return new UpaTargetResult
                {
                    Resolved = true,
                    GlobalObjectId = id,
                    Name = matches[0].name,
                    ScenePath = scene.path
                };
            }

            if (matches.Count > 1)
                return new UpaTargetResult
                {
                    Ambiguous = true,
                    Name = fallbackName,
                    ScenePath = scene.path,
                    Error = $"Ambiguous target: {matches.Count} GameObjects named '{fallbackName}'."
                };

            return Error($"GameObject '{fallbackName}' was not found.");
        }

        private static void CollectByName(
            Transform node,
            string name,
            List<GameObject> results)
        {
            if (node.name == name)
                results.Add(node.gameObject);

            for (var i = 0; i < node.childCount; i++)
                CollectByName(node.GetChild(i), name, results);
        }

        private static UpaTargetResult Error(string message)
            => new()
            {
                Resolved = false,
                Error = message
            };
    }
}
#endif
