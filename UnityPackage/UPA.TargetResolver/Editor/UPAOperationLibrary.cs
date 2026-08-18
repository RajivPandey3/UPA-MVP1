#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPA.TargetResolver.Editor
{
    public enum UpaOperationKind
    {
        SetTransformProperty,
        SetComponentProperty,
        AddComponent,
        SetTag,
        SetLayer
    }

    [Serializable]
    public sealed class UpaOperation
    {
        public string OperationId;
        public UpaOperationKind Kind;
        public string GlobalObjectId;
        public string FallbackName;
        public string ComponentType;
        public string PropertyPath;
        public string StringValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }

    public sealed class UpaOperationLibrary
    {
        private readonly UpaTargetResolver _resolver = new();

        public bool Execute(
            Scene scene,
            UpaOperation operation,
            bool dryRun,
            List<string> errors)
        {
            var target = _resolver.Resolve(
                scene,
                operation.GlobalObjectId,
                operation.FallbackName);

            if (!target.Resolved)
            {
                errors.Add(target.Error ?? "Target resolution failed.");
                return false;
            }

            var objectId = GlobalObjectId.TryParse(
                target.GlobalObjectId, out var parsed)
                ? parsed
                : default;

            var go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsed)
                as GameObject;

            if (go == null)
            {
                errors.Add("Resolved target no longer exists.");
                return false;
            }

            if (dryRun)
                return true;

            switch (operation.Kind)
            {
                case UpaOperationKind.SetTransformProperty:
                    return SetTransform(go, operation, errors);

                case UpaOperationKind.SetComponentProperty:
                    return SetComponentProperty(go, operation, errors);

                case UpaOperationKind.AddComponent:
                    return AddComponent(go, operation, errors);

                case UpaOperationKind.SetTag:
                    return SetTag(go, operation, errors);

                case UpaOperationKind.SetLayer:
                    return SetLayer(go, operation, errors);

                default:
                    errors.Add("Operation kind is not allowlisted.");
                    return false;
            }
        }

        private static bool SetTransform(
            GameObject go,
            UpaOperation op,
            List<string> errors)
        {
            Undo.RecordObject(go.transform, "UPA Transform Change");

            switch (op.PropertyPath)
            {
                case "m_LocalPosition.x":
                    var p = go.transform.localPosition;
                    p.x = op.FloatValue;
                    go.transform.localPosition = p;
                    return true;

                case "m_LocalPosition.y":
                    p = go.transform.localPosition;
                    p.y = op.FloatValue;
                    go.transform.localPosition = p;
                    return true;

                case "m_LocalPosition.z":
                    p = go.transform.localPosition;
                    p.z = op.FloatValue;
                    go.transform.localPosition = p;
                    return true;

                case "m_LocalScale.x":
                    var s = go.transform.localScale;
                    s.x = op.FloatValue;
                    go.transform.localScale = s;
                    return true;

                case "m_LocalScale.y":
                    s = go.transform.localScale;
                    s.y = op.FloatValue;
                    go.transform.localScale = s;
                    return true;

                case "m_LocalScale.z":
                    s = go.transform.localScale;
                    s.z = op.FloatValue;
                    go.transform.localScale = s;
                    return true;

                default:
                    errors.Add(
                        $"Transform property '{op.PropertyPath}' is not allowlisted.");
                    return false;
            }
        }

        private static bool SetComponentProperty(
            GameObject go,
            UpaOperation op,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(op.ComponentType) ||
                string.IsNullOrWhiteSpace(op.PropertyPath))
            {
                errors.Add("ComponentType and PropertyPath are required.");
                return false;
            }

            var type = Type.GetType(op.ComponentType, false);

            if (type == null ||
                !typeof(Component).IsAssignableFrom(type))
            {
                errors.Add("Component type could not be resolved.");
                return false;
            }

            var component = go.GetComponent(type);

            if (component == null)
            {
                errors.Add($"Component '{type.FullName}' is not present.");
                return false;
            }

            // Explicit serialized-property allowlist only.
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(op.PropertyPath);

            if (property == null)
            {
                errors.Add(
                    $"Serialized property '{op.PropertyPath}' was not found.");
                return false;
            }

            Undo.RecordObject(component, "UPA Component Property Change");

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = op.IntValue;
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue = op.FloatValue;
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue = op.BoolValue;
                    break;

                case SerializedPropertyType.String:
                    property.stringValue = op.StringValue;
                    break;

                default:
                    errors.Add(
                        $"Serialized property type '{property.propertyType}' is not allowlisted.");
                    return false;
            }

            serialized.ApplyModifiedProperties();
            return true;
        }

        private static bool AddComponent(
            GameObject go,
            UpaOperation op,
            List<string> errors)
        {
            var type = Type.GetType(op.ComponentType, false);

            if (type == null ||
                !typeof(Component).IsAssignableFrom(type) ||
                type == typeof(Transform))
            {
                errors.Add("Component type is not allowlisted.");
                return false;
            }

            if (go.GetComponent(type) != null)
            {
                errors.Add("Target already contains this component.");
                return false;
            }

            Undo.AddComponent(go, type);
            return true;
        }

        private static bool SetTag(
            GameObject go,
            UpaOperation op,
            List<string> errors)
        {
            try
            {
                Undo.RecordObject(go, "UPA Tag Change");
                go.tag = op.StringValue;
                return true;
            }
            catch (UnityException ex)
            {
                errors.Add(ex.Message);
                return false;
            }
        }

        private static bool SetLayer(
            GameObject go,
            UpaOperation op,
            List<string> errors)
        {
            if (op.IntValue < 0 || op.IntValue > 31)
            {
                errors.Add("Layer must be 0..31.");
                return false;
            }

            Undo.RecordObject(go, "UPA Layer Change");
            go.layer = op.IntValue;
            return true;
        }
    }
}
#endif
