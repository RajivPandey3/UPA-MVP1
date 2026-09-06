using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPA.UnityExecutor.Editor;

public static class OutsiderUnityProbe
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, "Assets/OutsiderProof.unity");
            var executor = new UpaUnityExecutor();
            var mutations = new[] { new UpaUnityMutation {
                OperationId = "create-proof", Kind = UpaUnityMutationKind.CreateGameObject,
                TargetObjectName = "CreatedByUPA"
            } };
            var dry = executor.Execute("proof", scene.path, null, mutations, true);
            var dryVerified = dry.Success && GameObject.Find("CreatedByUPA") == null;
            var denied = executor.Execute("proof", scene.path, null, mutations, false);
            var deniedVerified = !denied.Success && GameObject.Find("CreatedByUPA") == null;
            var approval = new UpaUnityApprovalToken { PlanId = "proof", ApprovedBy = "Outsider verification", ExplicitlyApproved = true };
            var created = executor.Execute("proof", scene.path, approval, mutations, false);
            var createVerified = created.Success && GameObject.Find("CreatedByUPA") != null;
            var component = executor.Execute("proof", scene.path, approval, new[] { new UpaUnityMutation {
                OperationId = "add-body", Kind = UpaUnityMutationKind.AddComponent,
                TargetObjectName = "CreatedByUPA", ComponentTypeName = "Rigidbody"
            } }, false);
            var componentVerified = component.Success && GameObject.Find("CreatedByUPA").GetComponent<Rigidbody>() != null;
            var unknownMutation = new[] { new UpaUnityMutation {
                OperationId = "unknown", Kind = UpaUnityMutationKind.AddComponent,
                TargetObjectName = "CreatedByUPA", ComponentTypeName = "UnknownComponent"
            } };
            var unknownDry = executor.Execute("proof", scene.path, null, unknownMutation, true);
            var unknownDryRejected = !unknownDry.Success && unknownDry.Errors.Count > 0;
            var unknownReal = executor.Execute("proof", scene.path, approval, unknownMutation, false);
            var priorTransactionPreserved = !unknownReal.Success && unknownReal.RolledBack &&
                GameObject.Find("CreatedByUPA") != null && GameObject.Find("CreatedByUPA").GetComponent<Rigidbody>() != null;
            var catalogVerified = UpaUnityExecutor.ResolveComponentType("Rigidbody") == typeof(Rigidbody) &&
                UpaUnityExecutor.ResolveComponentType("UnityEngine.Rigidbody") == typeof(Rigidbody) &&
                UpaUnityExecutor.ResolveComponentType(typeof(Rigidbody).AssemblyQualifiedName) == typeof(Rigidbody) &&
                UpaUnityExecutor.ResolveComponentType("UnknownComponent") == null;
            EditorSceneManager.SaveScene(scene);
            var report = "Dry run prevented creation: " + dryVerified + "\nApproval required: " + deniedVerified
                + "\nObject actually created: " + createVerified + "\nRigidbody actually added: " + componentVerified
                + "\nUnknown component rejected in dry run: " + unknownDryRejected
                + "\nEarlier transaction survived rollback: " + priorTransactionPreserved
                + "\nComponent aliases verified: " + catalogVerified
                + "\nErrors: " + string.Join("; ", created.Errors) + "; " + string.Join("; ", component.Errors);
            File.WriteAllText("outsider-unity-results.txt", report);
            Debug.Log(report);
            EditorApplication.Exit(dryVerified && deniedVerified && createVerified && componentVerified &&
                unknownDryRejected && priorTransactionPreserved && catalogVerified ? 0 : 1);
        }
        catch (Exception exception)
        {
            File.WriteAllText("outsider-unity-results.txt", exception.ToString());
            Debug.LogException(exception);
            EditorApplication.Exit(2);
        }
    }
}
