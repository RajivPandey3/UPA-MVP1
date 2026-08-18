#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.UnityExecutor.Editor
{
    public enum UpaUnityMutationKind
    {
        CreateGameObject,
        SetTransform,
        AddComponent,
        SetTag,
        SetLayer,
        SaveScene
    }

    [Serializable]
    public sealed class UpaUnityMutation
    {
        public string OperationId;
        public UpaUnityMutationKind Kind;
        public string TargetObjectName;
        public string ParentObjectName;
        public string ComponentTypeName;
        public string Tag;
        public int Layer = -1;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
    }

    [Serializable]
    public sealed class UpaUnityAuditEntry
    {
        public string OperationId;
        public string Event;
        public string Detail;
        public string TimestampUtc;
    }

    public sealed class UpaUnityExecutionResult
    {
        public bool Success;
        public bool DryRun;
        public bool RolledBack;
        public List<UpaUnityAuditEntry> Audit = new();
        public List<string> Errors = new();
    }

    public sealed class UpaUnityApprovalToken
    {
        public string PlanId;
        public string ApprovedBy;
        public bool ExplicitlyApproved;
    }

    public sealed class UpaUnityExecutor
    {
        public UpaUnityExecutionResult Execute(
            string planId,
            string scenePath,
            UpaUnityApprovalToken approval,
            IReadOnlyList<UpaUnityMutation> mutations,
            bool dryRun)
        {
            var result = new UpaUnityExecutionResult
            {
                Success = false,
                DryRun = dryRun
            };

            Audit(result, planId, "TransactionStarted",
                dryRun ? "Dry-run." : "Mutation execution.");

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Fail(result, "UNITY-EXEC-001",
                    "An explicit target scene path is required.");
                return result;
            }

            if (!dryRun &&
                (approval == null ||
                 !approval.ExplicitlyApproved ||
                 !string.Equals(approval.PlanId, planId,
                     StringComparison.Ordinal)))
            {
                Fail(result, "UNITY-EXEC-002",
                    "Explicit approval for this plan is required.");
                return result;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Fail(result, "UNITY-EXEC-003",
                    $"Target scene is not loaded: {scenePath}");
                return result;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("UPA Plan " + planId);

            try
            {
                foreach (var mutation in mutations)
                {
                    ValidateMutation(mutation, result);

                    if (result.Errors.Count > 0)
                        break;

                    if (dryRun)
                    {
                        Audit(result, mutation.OperationId,
                            "MutationValidated", mutation.Kind.ToString());
                        continue;
                    }

                    ApplyMutation(scene, mutation, result);

                    if (result.Errors.Count > 0)
                        break;
                }

                if (result.Errors.Count > 0 && !dryRun)
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    result.RolledBack = true;
                    Audit(result, planId, "RollbackCompleted",
                        "Unity Undo reverted the transaction.");
                    return result;
                }

                if (!dryRun)
                {
                    Undo.CollapseUndoOperations(undoGroup);
                    Audit(result, planId, "TransactionCommitted",
                        "Unity mutation transaction committed to Undo.");
                }

                result.Success = true;
                Audit(result, planId, "TransactionCompleted",
                    dryRun ? "Dry-run completed." : "Completed.");
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);

                if (!dryRun)
                {
                    try
                    {
                        Undo.RevertAllDownToGroup(undoGroup);
                        result.RolledBack = true;
                    }
                    catch (Exception undoEx)
                    {
                        result.Errors.Add("Undo rollback failed: " + undoEx.Message);
                    }
                }

                Audit(result, planId, "TransactionFailed", ex.Message);
                return result;
            }
        }

        private static void ValidateMutation(
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            if (mutation == null)
            {
                result.Errors.Add("UNITY-EXEC-010: Null mutation.");
                return;
            }

            if (string.IsNullOrWhiteSpace(mutation.OperationId))
                result.Errors.Add("UNITY-EXEC-011: OperationId is required.");

            if (mutation.Kind != UpaUnityMutationKind.SaveScene &&
                string.IsNullOrWhiteSpace(mutation.TargetObjectName))
            {
                result.Errors.Add(
                    $"UNITY-EXEC-012: TargetObjectName is required for {mutation.Kind}.");
            }

            if (mutation.Kind == UpaUnityMutationKind.AddComponent &&
                string.IsNullOrWhiteSpace(mutation.ComponentTypeName))
            {
                result.Errors.Add(
                    "UNITY-EXEC-013: ComponentTypeName is required.");
            }

            if (mutation.Kind == UpaUnityMutationKind.SetTag &&
                string.IsNullOrWhiteSpace(mutation.Tag))
            {
                result.Errors.Add("UNITY-EXEC-014: Tag is required.");
            }

            if (mutation.Kind == UpaUnityMutationKind.SetLayer &&
                (mutation.Layer < 0 || mutation.Layer > 31))
            {
                result.Errors.Add("UNITY-EXEC-015: Layer must be 0..31.");
            }
        }

        private static void ApplyMutation(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            switch (mutation.Kind)
            {
                case UpaUnityMutationKind.CreateGameObject:
                    CreateGameObject(scene, mutation, result);
                    break;

                case UpaUnityMutationKind.SetTransform:
                    SetTransform(scene, mutation, result);
                    break;

                case UpaUnityMutationKind.AddComponent:
                    AddComponent(scene, mutation, result);
                    break;

                case UpaUnityMutationKind.SetTag:
                    SetTag(scene, mutation, result);
                    break;

                case UpaUnityMutationKind.SetLayer:
                    SetLayer(scene, mutation, result);
                    break;

                case UpaUnityMutationKind.SaveScene:
                    if (!EditorSceneManager.SaveScene(scene))
                        result.Errors.Add(
                            "UNITY-EXEC-020: Unity failed to save the target scene.");
                    break;

                default:
                    result.Errors.Add(
                        "UNITY-EXEC-021: Mutation kind is not allowlisted.");
                    break;
            }
        }

        private static GameObject FindTarget(
            Scene scene, string name, UpaUnityExecutionResult result)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            result.Errors.Add(
                $"UNITY-EXEC-030: GameObject '{name}' was not found.");
            return null;
        }

        private static Transform FindRecursive(Transform node, string name)
        {
            if (node.name == name)
                return node;

            for (var i = 0; i < node.childCount; i++)
            {
                var found = FindRecursive(node.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void CreateGameObject(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            if (FindRecursiveAny(scene, mutation.TargetObjectName) != null)
            {
                result.Errors.Add(
                    $"UNITY-EXEC-031: GameObject '{mutation.TargetObjectName}' already exists.");
                return;
            }

            var go = new GameObject(mutation.TargetObjectName);
            Undo.RegisterCreatedObjectUndo(go, "UPA Create GameObject");
            SceneManager.MoveGameObjectToScene(go, scene);

            if (!string.IsNullOrWhiteSpace(mutation.ParentObjectName))
            {
                var parent = FindTarget(scene, mutation.ParentObjectName, result);
                if (parent == null)
                    return;

                Undo.SetTransformParent(
                    go.transform,
                    parent.transform,
                    "UPA Set Parent");
            }

            go.transform.localPosition = mutation.Position;
            go.transform.localRotation = mutation.Rotation;
            go.transform.localScale = mutation.Scale;

            Audit(result, mutation.OperationId,
                "MutationApplied",
                "Created GameObject '" + mutation.TargetObjectName + "'.");
        }

        private static void SetTransform(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            var go = FindTarget(scene, mutation.TargetObjectName, result);
            if (go == null) return;

            Undo.RecordObject(go.transform, "UPA Set Transform");
            go.transform.localPosition = mutation.Position;
            go.transform.localRotation = mutation.Rotation;
            go.transform.localScale = mutation.Scale;

            Audit(result, mutation.OperationId,
                "MutationApplied", "Transform updated.");
        }

        private static void AddComponent(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            var go = FindTarget(scene, mutation.TargetObjectName, result);
            if (go == null) return;

            var type = Type.GetType(mutation.ComponentTypeName, false);
            if (type == null ||
                !typeof(Component).IsAssignableFrom(type) ||
                type == typeof(Transform))
            {
                result.Errors.Add(
                    $"UNITY-EXEC-040: Component type is not allowlisted/resolvable: {mutation.ComponentTypeName}");
                return;
            }

            Undo.AddComponent(go, type);
            Audit(result, mutation.OperationId,
                "MutationApplied",
                "Added component " + type.FullName + ".");
        }

        private static void SetTag(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            var go = FindTarget(scene, mutation.TargetObjectName, result);
            if (go == null) return;

            try
            {
                Undo.RecordObject(go, "UPA Set Tag");
                go.tag = mutation.Tag;
                Audit(result, mutation.OperationId,
                    "MutationApplied", "Tag set to " + mutation.Tag + ".");
            }
            catch (UnityException ex)
            {
                result.Errors.Add(
                    "UNITY-EXEC-050: Tag assignment failed: " + ex.Message);
            }
        }

        private static void SetLayer(
            Scene scene,
            UpaUnityMutation mutation,
            UpaUnityExecutionResult result)
        {
            var go = FindTarget(scene, mutation.TargetObjectName, result);
            if (go == null) return;

            Undo.RecordObject(go, "UPA Set Layer");
            go.layer = mutation.Layer;

            Audit(result, mutation.OperationId,
                "MutationApplied",
                "Layer set to " + mutation.Layer + ".");
        }

        private static GameObject FindRecursiveAny(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static void Audit(
            UpaUnityExecutionResult result,
            string operationId,
            string @event,
            string detail)
        {
            result.Audit.Add(new UpaUnityAuditEntry
            {
                OperationId = operationId,
                Event = @event,
                Detail = detail,
                TimestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        private static void Fail(
            UpaUnityExecutionResult result,
            string code,
            string message)
        {
            result.Errors.Add(code + ": " + message);
            Audit(result, code, "Rejected", message);
        }
    }
}
#endif
