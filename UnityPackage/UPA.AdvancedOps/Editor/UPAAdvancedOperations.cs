#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.AdvancedOps.Editor
{
    public enum UpaAdvancedOperationKind
    {
        AssignMaterial,
        ConfigureRigidbody,
        AddBoxCollider,
        AddSphereCollider,
        SavePrefabAsset,
        CreateScriptableObject,
        SetTag,
        SetLayer
    }

    [Serializable]
    public sealed class UpaAdvancedOperation
    {
        public string OperationId;
        public UpaAdvancedOperationKind Kind;

        // Target by GlobalObjectId wherever possible.
        public string TargetGlobalObjectId;
        public string FallbackTargetName;

        // Asset target.
        public string AssetPath;

        // Material.
        public int MaterialSlot;

        // Rigidbody.
        public bool UseGravity = true;
        public bool IsKinematic;
        public float Mass = 1f;
        public float Drag;
        public float AngularDrag = 0.05f;

        // Tag/layer.
        public string StringValue;
        public int IntValue;

        // ScriptableObject type.
        public string ScriptableObjectTypeName;
    }

    public sealed class UpaAdvancedOperationResult
    {
        public bool Success;
        public List<string> Errors = new();
        public List<string> Audit = new();
    }

    public sealed class UpaAdvancedOperations
    {
        public UpaAdvancedOperationResult Execute(
            Scene scene,
            UpaAdvancedOperation operation,
            bool dryRun)
        {
            var result = new UpaAdvancedOperationResult();

            if (operation == null)
            {
                result.Errors.Add("ADV-001: Operation is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(operation.OperationId))
            {
                result.Errors.Add("ADV-002: OperationId is required.");
                return result;
            }

            try
            {
                switch (operation.Kind)
                {
                    case UpaAdvancedOperationKind.AssignMaterial:
                        AssignMaterial(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.ConfigureRigidbody:
                        ConfigureRigidbody(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.AddBoxCollider:
                        AddCollider<BoxCollider>(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.AddSphereCollider:
                        AddCollider<SphereCollider>(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.SavePrefabAsset:
                        SavePrefab(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.CreateScriptableObject:
                        CreateScriptableObject(operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.SetTag:
                        SetTag(scene, operation, dryRun, result);
                        break;

                    case UpaAdvancedOperationKind.SetLayer:
                        SetLayer(scene, operation, dryRun, result);
                        break;

                    default:
                        result.Errors.Add("ADV-003: Operation kind is not allowlisted.");
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add("ADV-999: " + ex.Message);
            }

            result.Success = result.Errors.Count == 0;
            return result;
        }

        private static GameObject ResolveTarget(
            Scene scene,
            UpaAdvancedOperation op,
            UpaAdvancedOperationResult result)
        {
            if (!string.IsNullOrWhiteSpace(op.TargetGlobalObjectId) &&
                GlobalObjectId.TryParse(op.TargetGlobalObjectId, out var id))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                if (obj is GameObject go && go.scene == scene)
                    return go;
            }

            if (string.IsNullOrWhiteSpace(op.FallbackTargetName))
            {
                result.Errors.Add(
                    "ADV-010: Exact target ID is unavailable and no fallback name was supplied.");
                return null;
            }

            GameObject found = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var matches = FindAll(root.transform, op.FallbackTargetName);
                if (matches.Count > 1)
                {
                    result.Errors.Add(
                        $"ADV-011: Ambiguous target '{op.FallbackTargetName}'.");
                    return null;
                }

                if (matches.Count == 1)
                {
                    if (found != null)
                    {
                        result.Errors.Add(
                            $"ADV-011: Ambiguous target '{op.FallbackTargetName}'.");
                        return null;
                    }

                    found = matches[0];
                }
            }

            if (found == null)
                result.Errors.Add(
                    $"ADV-012: Target '{op.FallbackTargetName}' not found.");

            return found;
        }

        private static List<GameObject> FindAll(
            Transform node,
            string name)
        {
            var result = new List<GameObject>();

            if (node.name == name)
                result.Add(node.gameObject);

            for (var i = 0; i < node.childCount; i++)
                result.AddRange(FindAll(node.GetChild(i), name));

            return result;
        }

        private static void AssignMaterial(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            if (op.MaterialSlot < 0)
            {
                result.Errors.Add("ADV-020: Material slot cannot be negative.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(op.AssetPath);

            if (material == null)
            {
                result.Errors.Add(
                    $"ADV-021: Material asset could not be resolved: {op.AssetPath}");
                return;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                result.Errors.Add("ADV-022: Target has no Renderer.");
                return;
            }

            if (op.MaterialSlot >= renderer.sharedMaterials.Length)
            {
                result.Errors.Add("ADV-023: Material slot is outside renderer bounds.");
                return;
            }

            if (dryRun)
            {
                result.Audit.Add(
                    $"Validated material assignment {op.AssetPath} → {go.name}[{op.MaterialSlot}]");
                return;
            }

            Undo.RecordObject(renderer, "UPA Assign Material");
            var materials = renderer.sharedMaterials;
            materials[op.MaterialSlot] = material;
            renderer.sharedMaterials = materials;

            result.Audit.Add("Material assignment applied.");
        }

        private static void ConfigureRigidbody(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            var rb = go.GetComponent<Rigidbody>();

            if (dryRun)
            {
                result.Audit.Add("Validated Rigidbody configuration.");
                return;
            }

            if (rb == null)
                rb = Undo.AddComponent<Rigidbody>(go);

            Undo.RecordObject(rb, "UPA Configure Rigidbody");

            rb.useGravity = op.UseGravity;
            rb.isKinematic = op.IsKinematic;
            rb.mass = Mathf.Max(0.0001f, op.Mass);
            rb.linearDamping = Mathf.Max(0f, op.Drag);
            rb.angularDamping = Mathf.Max(0f, op.AngularDrag);

            result.Audit.Add("Rigidbody configured.");
        }

        private static void AddCollider<T>(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
            where T : Collider
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            if (go.GetComponent<T>() != null)
            {
                result.Errors.Add(
                    $"ADV-030: Target already has {typeof(T).Name}.");
                return;
            }

            if (dryRun)
            {
                result.Audit.Add(
                    $"Validated add {typeof(T).Name}.");
                return;
            }

            Undo.AddComponent<T>(go);
            result.Audit.Add($"Added {typeof(T).Name}.");
        }

        private static void SavePrefab(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            if (string.IsNullOrWhiteSpace(op.AssetPath) ||
                !op.AssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                result.Errors.Add(
                    "ADV-040: Prefab path must be inside Assets/.");
                return;
            }

            if (dryRun)
            {
                result.Audit.Add(
                    $"Validated prefab save: {go.name} → {op.AssetPath}");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(go, op.AssetPath);
            result.Audit.Add("Prefab asset saved.");
        }

        private static void CreateScriptableObject(
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath) ||
                !op.AssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                result.Errors.Add(
                    "ADV-050: ScriptableObject path must be inside Assets/.");
                return;
            }

            var type = Type.GetType(op.ScriptableObjectTypeName, false);

            if (type == null ||
                !typeof(ScriptableObject).IsAssignableFrom(type))
            {
                result.Errors.Add(
                    "ADV-051: ScriptableObject type is not resolvable/allowlisted.");
                return;
            }

            if (dryRun)
            {
                result.Audit.Add(
                    $"Validated ScriptableObject creation: {type.FullName}");
                return;
            }

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, op.AssetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(asset);

            result.Audit.Add(
                $"Created ScriptableObject asset: {op.AssetPath}");
        }

        private static void SetTag(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            if (dryRun)
            {
                result.Audit.Add("Validated tag assignment.");
                return;
            }

            try
            {
                Undo.RecordObject(go, "UPA Set Tag");
                go.tag = op.StringValue;
                result.Audit.Add("Tag assigned.");
            }
            catch (UnityException ex)
            {
                result.Errors.Add("ADV-060: " + ex.Message);
            }
        }

        private static void SetLayer(
            Scene scene,
            UpaAdvancedOperation op,
            bool dryRun,
            UpaAdvancedOperationResult result)
        {
            var go = ResolveTarget(scene, op, result);
            if (go == null) return;

            if (op.IntValue < 0 || op.IntValue > 31)
            {
                result.Errors.Add("ADV-061: Layer must be 0..31.");
                return;
            }

            if (dryRun)
            {
                result.Audit.Add("Validated layer assignment.");
                return;
            }

            Undo.RecordObject(go, "UPA Set Layer");
            go.layer = op.IntValue;
            result.Audit.Add("Layer assigned.");
        }
    }
}
#endif
