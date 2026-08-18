#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPA.SettingsOps.Editor
{
    public enum UpaSettingsOperationKind
    {
        SetPhysicsGravity,
        SetPhysicsFixedTimestep,
        SetProductName,
        SetCompanyName,
        SetVSyncCount,
        SetAntiAliasing,
        SetShadowDistance,
        SetTextureMaxSize,
        SetTextureCompression,
        SetModelImportMaterials,
        SetModelAnimationType,
        SetAudioLoadInBackground
    }

    public enum UpaTextureCompression
    {
        Uncompressed,
        Compressed,
        Crunch
    }

    public enum UpaModelAnimationType
    {
        None,
        Legacy,
        Generic,
        Human
    }

    [Serializable]
    public sealed class UpaSettingsOperation
    {
        public string OperationId;
        public UpaSettingsOperationKind Kind;

        // Typed values.
        public Vector3 Vector3Value;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        public string StringValue;

        // Asset importer target.
        public string AssetPath;

        public UpaTextureCompression TextureCompression;
        public UpaModelAnimationType ModelAnimationType;
    }

    public sealed class UpaSettingsOperationResult
    {
        public bool Success;
        public List<string> Errors = new();
        public List<string> Audit = new();
    }

    public sealed class UpaSettingsOperations
    {
        public UpaSettingsOperationResult Execute(
            UpaSettingsOperation operation,
            bool dryRun)
        {
            var result = new UpaSettingsOperationResult();

            if (operation == null)
            {
                result.Errors.Add("SET-001: Operation is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(operation.OperationId))
            {
                result.Errors.Add("SET-002: OperationId is required.");
                return result;
            }

            try
            {
                switch (operation.Kind)
                {
                    case UpaSettingsOperationKind.SetPhysicsGravity:
                        SetPhysicsGravity(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetPhysicsFixedTimestep:
                        SetPhysicsFixedTimestep(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetProductName:
                        SetProductName(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetCompanyName:
                        SetCompanyName(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetVSyncCount:
                        SetVSyncCount(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetAntiAliasing:
                        SetAntiAliasing(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetShadowDistance:
                        SetShadowDistance(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetTextureMaxSize:
                        SetTextureMaxSize(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetTextureCompression:
                        SetTextureCompression(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetModelImportMaterials:
                        SetModelImportMaterials(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetModelAnimationType:
                        SetModelAnimationType(operation, dryRun, result);
                        break;

                    case UpaSettingsOperationKind.SetAudioLoadInBackground:
                        SetAudioLoadInBackground(operation, dryRun, result);
                        break;

                    default:
                        result.Errors.Add("SET-003: Operation kind is not allowlisted.");
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add("SET-999: " + ex.Message);
            }

            result.Success = result.Errors.Count == 0;
            return result;
        }

        private static void SetPhysicsGravity(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            ValidateFinite(op.Vector3Value, "gravity", r);
            if (r.Errors.Count > 0) return;

            if (dryRun)
            {
                r.Audit.Add("Validated Physics.gravity.");
                return;
            }

            Undo.RecordObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/PhysicsManager.asset")[0],
                "UPA Set Physics Gravity");

            Physics.gravity = op.Vector3Value;
            r.Audit.Add("Physics.gravity updated.");
        }

        private static void SetPhysicsFixedTimestep(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (!IsPositiveFinite(op.FloatValue))
            {
                r.Errors.Add("SET-010: Fixed timestep must be finite and > 0.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated Time.fixedDeltaTime.");
                return;
            }

            Time.fixedDeltaTime = op.FloatValue;
            r.Audit.Add("Time.fixedDeltaTime updated.");
        }

        private static void SetProductName(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.StringValue))
            {
                r.Errors.Add("SET-020: Product name cannot be empty.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated PlayerSettings.productName.");
                return;
            }

            PlayerSettings.productName = op.StringValue;
            r.Audit.Add("PlayerSettings.productName updated.");
        }

        private static void SetCompanyName(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.StringValue))
            {
                r.Errors.Add("SET-021: Company name cannot be empty.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated PlayerSettings.companyName.");
                return;
            }

            PlayerSettings.companyName = op.StringValue;
            r.Audit.Add("PlayerSettings.companyName updated.");
        }

        private static void SetVSyncCount(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (op.IntValue < 0 || op.IntValue > 4)
            {
                r.Errors.Add("SET-030: VSync count must be 0..4.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated QualitySettings.vSyncCount.");
                return;
            }

            QualitySettings.vSyncCount = op.IntValue;
            r.Audit.Add("QualitySettings.vSyncCount updated.");
        }

        private static void SetAntiAliasing(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (op.IntValue != 0 &&
                op.IntValue != 2 &&
                op.IntValue != 4 &&
                op.IntValue != 8)
            {
                r.Errors.Add("SET-031: Anti-aliasing must be 0, 2, 4 or 8.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated QualitySettings.antiAliasing.");
                return;
            }

            QualitySettings.antiAliasing = op.IntValue;
            r.Audit.Add("QualitySettings.antiAliasing updated.");
        }

        private static void SetShadowDistance(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (!IsPositiveOrZeroFinite(op.FloatValue))
            {
                r.Errors.Add("SET-032: Shadow distance must be finite and >= 0.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated QualitySettings.shadowDistance.");
                return;
            }

            QualitySettings.shadowDistance = op.FloatValue;
            r.Audit.Add("QualitySettings.shadowDistance updated.");
        }

        private static void SetTextureMaxSize(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath))
            {
                r.Errors.Add("SET-040: Texture asset path is required.");
                return;
            }

            var importer = AssetImporter.GetAtPath(op.AssetPath) as TextureImporter;
            if (importer == null)
            {
                r.Errors.Add("SET-041: Target is not a TextureImporter asset.");
                return;
            }

            if (op.IntValue < 32 || op.IntValue > 16384)
            {
                r.Errors.Add("SET-042: Texture max size must be 32..16384.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated TextureImporter.maxTextureSize.");
                return;
            }

            importer.maxTextureSize = op.IntValue;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            r.Audit.Add("Texture importer max size updated and reimported.");
        }

        private static void SetTextureCompression(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath))
            {
                r.Errors.Add("SET-043: Texture asset path is required.");
                return;
            }

            var importer = AssetImporter.GetAtPath(op.AssetPath) as TextureImporter;
            if (importer == null)
            {
                r.Errors.Add("SET-044: Target is not a TextureImporter asset.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated TextureImporter compression policy.");
                return;
            }

            importer.textureCompression = op.TextureCompression switch
            {
                UpaTextureCompression.Uncompressed =>
                    TextureImporterCompression.Uncompressed,
                UpaTextureCompression.Compressed =>
                    TextureImporterCompression.Compressed,
                UpaTextureCompression.Crunch =>
                    TextureImporterCompression.CompressedHQ,
                _ => TextureImporterCompression.Compressed
            };

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            r.Audit.Add("Texture compression policy updated and reimported.");
        }

        private static void SetModelImportMaterials(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath))
            {
                r.Errors.Add("SET-050: Model asset path is required.");
                return;
            }

            var importer = AssetImporter.GetAtPath(op.AssetPath) as ModelImporter;
            if (importer == null)
            {
                r.Errors.Add("SET-051: Target is not a ModelImporter asset.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated ModelImporter material import policy.");
                return;
            }

            importer.materialImportMode =
                op.BoolValue
                    ? ModelImporterMaterialImportMode.ImportStandard
                    : ModelImporterMaterialImportMode.None;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            r.Audit.Add("Model material import policy updated and reimported.");
        }

        private static void SetModelAnimationType(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath))
            {
                r.Errors.Add("SET-052: Model asset path is required.");
                return;
            }

            var importer = AssetImporter.GetAtPath(op.AssetPath) as ModelImporter;
            if (importer == null)
            {
                r.Errors.Add("SET-053: Target is not a ModelImporter asset.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated ModelImporter animation type.");
                return;
            }

            importer.animationType = op.ModelAnimationType switch
            {
                UpaModelAnimationType.None => ModelImporterAnimationType.None,
                UpaModelAnimationType.Legacy => ModelImporterAnimationType.Legacy,
                UpaModelAnimationType.Generic => ModelImporterAnimationType.Generic,
                UpaModelAnimationType.Human => ModelImporterAnimationType.Human,
                _ => ModelImporterAnimationType.None
            };

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            r.Audit.Add("Model animation type updated and reimported.");
        }

        private static void SetAudioLoadInBackground(
            UpaSettingsOperation op, bool dryRun, UpaSettingsOperationResult r)
        {
            if (string.IsNullOrWhiteSpace(op.AssetPath))
            {
                r.Errors.Add("SET-060: Audio asset path is required.");
                return;
            }

            var importer = AssetImporter.GetAtPath(op.AssetPath) as AudioImporter;
            if (importer == null)
            {
                r.Errors.Add("SET-061: Target is not an AudioImporter asset.");
                return;
            }

            if (dryRun)
            {
                r.Audit.Add("Validated AudioImporter.loadInBackground.");
                return;
            }

            importer.loadInBackground = op.BoolValue;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            r.Audit.Add("Audio load-in-background policy updated and reimported.");
        }

        private static bool IsPositiveFinite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private static bool IsPositiveOrZeroFinite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

        private static void ValidateFinite(
            Vector3 value, string label, UpaSettingsOperationResult r)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z))
            {
                r.Errors.Add($"SET-100: {label} contains non-finite values.");
            }
        }
    }
}
#endif
