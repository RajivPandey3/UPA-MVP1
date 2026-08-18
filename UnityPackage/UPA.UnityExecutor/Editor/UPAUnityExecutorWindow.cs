#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UPA.UnityExecutor.Editor
{
    public sealed class UpaUnityExecutorWindow : EditorWindow
    {
        private string _status = "Ready.";
        private bool _dryRun = true;

        [MenuItem("Tools/UPA/Execution/Unity Executor")]
        public static void Open()
        {
            GetWindow<UpaUnityExecutorWindow>("UPA Unity Executor");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "UPA Unity Executor v1.0",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Governed mutation surface. Dry-run is the default. "
                + "All real mutations go through Unity Undo.",
                MessageType.Warning);

            _dryRun = EditorGUILayout.ToggleLeft("Dry Run", _dryRun);

            if (GUILayout.Button("Test Empty Transaction"))
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    _status = "No loaded active scene.";
                    return;
                }

                var executor = new UpaUnityExecutor();
                var result = executor.Execute(
                    "manual-test",
                    scene.path,
                    _dryRun
                        ? null
                        : new UpaUnityApprovalToken
                        {
                            PlanId = "manual-test",
                            ApprovedBy = "Editor User",
                            ExplicitlyApproved = true
                        },
                    new List<UpaUnityMutation>(),
                    _dryRun);

                _status = result.Success
                    ? "Transaction accepted."
                    : "Transaction rejected.";
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", _status);
        }
    }
}
#endif
