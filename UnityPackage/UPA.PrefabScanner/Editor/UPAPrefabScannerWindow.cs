#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPA.PrefabScanner.Editor
{
    public sealed class UpaPrefabScannerWindow : EditorWindow
    {
        private int _prefabs;
        private int _objects;
        private int _missingScripts;
        private int _dependencies;

        [MenuItem("Tools/UPA/X-Ray/Prefab Scanner")]
        public static void Open()
        {
            GetWindow<UpaPrefabScannerWindow>("UPA Prefab X-Ray");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "UPA Prefab Scanner v1.0",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Read-only scan. Prefabs are loaded temporarily and unloaded without saving.",
                MessageType.Info);

            if (GUILayout.Button("Scan All Prefabs"))
                RunScan();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Prefabs", _prefabs.ToString());
            EditorGUILayout.LabelField("GameObjects", _objects.ToString());
            EditorGUILayout.LabelField("Missing Scripts", _missingScripts.ToString());
            EditorGUILayout.LabelField("Nested Dependencies", _dependencies.ToString());
        }

        private void RunScan()
        {
            var scanner = new UpaPrefabScanner();
            var result = scanner.ScanProjectPrefabs();

            _prefabs = result.Count;
            _objects = 0;
            _missingScripts = 0;
            _dependencies = 0;

            foreach (var prefab in result)
            {
                _dependencies += prefab.Dependencies.Count;
                foreach (var root in prefab.Roots)
                    Count(root);
            }

            Repaint();
        }

        private void Count(UpaPrefabObjectSnapshot obj)
        {
            _objects++;

            foreach (var component in obj.Components)
                if (component.MissingScript)
                    _missingScripts++;

            foreach (var child in obj.Children)
                Count(child);
        }
    }
}
#endif
