#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPA.SceneScanner.Editor
{
    public sealed class UpaSceneScannerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _status = "Ready.";
        private int _sceneCount;
        private int _objectCount;
        private int _missingScripts;

        [MenuItem("Tools/UPA/X-Ray/Scene Scanner")]
        public static void Open()
        {
            GetWindow<UpaSceneScannerWindow>("UPA Scene X-Ray");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "UPA Scene Scanner v1.0",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Read-only scan. No scene is saved and no project object is modified.",
                MessageType.Info);

            if (GUILayout.Button("Scan All Scene Assets"))
                RunScan();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", _status);
            EditorGUILayout.LabelField("Scenes", _sceneCount.ToString());
            EditorGUILayout.LabelField("GameObjects", _objectCount.ToString());
            EditorGUILayout.LabelField("Missing Scripts", _missingScripts.ToString());

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.EndScrollView();
        }

        private void RunScan()
        {
            var scanner = new UpaSceneScanner();
            var scenes = scanner.ScanProjectScenes();

            _sceneCount = scenes.Count;
            _objectCount = 0;
            _missingScripts = 0;

            foreach (var scene in scenes)
            {
                foreach (var root in scene.Roots)
                    Count(root);
            }

            _status = "Scan complete — read-only.";
            Repaint();
        }

        private void Count(UpaGameObjectSnapshot go)
        {
            _objectCount++;
            foreach (var c in go.Components)
                if (c.MissingScript) _missingScripts++;

            foreach (var child in go.Children)
                Count(child);
        }
    }
}
#endif
