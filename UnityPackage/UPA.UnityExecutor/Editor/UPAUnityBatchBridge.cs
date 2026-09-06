#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UPA.UnityExecutor.Editor
{
    public static class UpaUnityBatchBridge
    {
        [Serializable]
        private sealed class Request
        {
            public string mode;
            public string scenePath;
            public string objectName;
            public string planId;
            public string approvedBy;
            public string resultPath;
        }

        [Serializable]
        private sealed class Result
        {
            public bool verified;
            public string planId;
        }

        public static void Run()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode is required.");
                var arguments = Environment.GetCommandLineArgs();
                var index = Array.IndexOf(arguments, "-upaRequest");
                if (index < 0 || index + 1 >= arguments.Length) throw new InvalidOperationException("Missing request.");
                var request = JsonUtility.FromJson<Request>(File.ReadAllText(arguments[index + 1]));
                if (request == null || string.IsNullOrWhiteSpace(request.planId) ||
                    !Regex.IsMatch(request.scenePath ?? "", @"\AAssets/(?:[A-Za-z0-9_-]+/)*[A-Za-z0-9_-]+\.unity\z") ||
                    !Regex.IsMatch(request.objectName ?? "", @"\A[A-Za-z][A-Za-z0-9_]{0,63}\z"))
                    throw new InvalidOperationException("Invalid bound request.");
                if (request.mode == "execute")
                {
                    if (string.IsNullOrWhiteSpace(request.approvedBy)) throw new InvalidOperationException("Approval required.");
                    if (File.Exists(request.scenePath)) throw new InvalidOperationException("Target scene already exists.");
                    Directory.CreateDirectory(Path.GetDirectoryName(request.scenePath));
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    if (!EditorSceneManager.SaveScene(scene, request.scenePath)) throw new IOException("Initial scene save failed.");
                    var result = new UpaUnityExecutor().Execute(request.planId, request.scenePath,
                        new UpaUnityApprovalToken { PlanId = request.planId, ApprovedBy = request.approvedBy, ExplicitlyApproved = true },
                        new[] {
                            new UpaUnityMutation { OperationId = "create", Kind = UpaUnityMutationKind.CreateGameObject, TargetObjectName = request.objectName },
                            new UpaUnityMutation { OperationId = "body", Kind = UpaUnityMutationKind.AddComponent, TargetObjectName = request.objectName, ComponentTypeName = "Rigidbody" }
                        }, false);
                    if (!result.Success) throw new InvalidOperationException(string.Join("; ", result.Errors));
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Final scene save failed.");
                }
                else if (request.mode != "verify") throw new InvalidOperationException("Unknown mode.");
                var reopened = EditorSceneManager.OpenScene(request.scenePath, OpenSceneMode.Single);
                var roots = reopened.GetRootGameObjects();
                var target = roots.SingleOrDefault(candidate => candidate.name == request.objectName);
                if (roots.Length != 1 || target == null || target.GetComponent<Rigidbody>() == null)
                    throw new InvalidOperationException("Reopened scene does not match the approved plan.");
                File.WriteAllText(request.resultPath, JsonUtility.ToJson(new Result { verified = true, planId = request.planId }));
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
