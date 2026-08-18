#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPA.AssetScanner.Editor
{
    public sealed class UpaAssetScannerWindow : EditorWindow
    {
        private int _assets;
        private long _bytes;
        private int _dependencies;
        private int _diagnostics;

        [MenuItem("Tools/UPA/X-Ray/Asset Scanner")]
        public static void Open()
        {
            GetWindow<UpaAssetScannerWindow>("UPA Asset X-Ray");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "UPA Asset Scanner v1.0",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Read-only AssetDatabase scan. Importer settings and assets are never modified.",
                MessageType.Info);

            if (GUILayout.Button("Scan Project Assets"))
                RunScan();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Assets", _assets.ToString());
            EditorGUILayout.LabelField("File Bytes", _bytes.ToString());
            EditorGUILayout.LabelField("Dependencies", _dependencies.ToString());
            EditorGUILayout.LabelField("Diagnostics", _diagnostics.ToString());
        }

        private void RunScan()
        {
            var result = new UpaAssetScanner().ScanProjectAssets();

            _assets = result.Count;
            _bytes = 0;
            _dependencies = 0;
            _diagnostics = 0;

            foreach (var asset in result)
            {
                _bytes += asset.FileSize;
                _dependencies += asset.Dependencies.Count;
                _diagnostics += asset.Diagnostics.Count;
            }

            Repaint();
        }
    }
}
#endif
