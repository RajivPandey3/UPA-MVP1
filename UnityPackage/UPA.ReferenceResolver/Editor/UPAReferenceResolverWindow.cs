#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPA.ReferenceResolver.Editor
{
    public sealed class UpaReferenceResolverWindow : EditorWindow
    {
        private int _edges;
        private int _reverse;
        private int _unresolved;

        [MenuItem("Tools/UPA/X-Ray/Reference Resolver")]
        public static void Open()
        {
            GetWindow<UpaReferenceResolverWindow>("UPA Reference X-Ray");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "UPA Reference Resolver v1.0",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Read-only dependency graph. No Unity object or asset is modified.",
                MessageType.Info);

            if (GUILayout.Button("Build Asset Reference Graph"))
                RunScan();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Edges", _edges.ToString());
            EditorGUILayout.LabelField("Reverse References", _reverse.ToString());
            EditorGUILayout.LabelField("Unresolved", _unresolved.ToString());
        }

        private void RunScan()
        {
            var graph = new UpaReferenceResolver().BuildAssetGraph();

            _edges = graph.Edges.Count;
            _reverse = graph.ReverseReferences.Count;
            _unresolved = 0;

            foreach (var edge in graph.Edges)
                if (!edge.Resolved)
                    _unresolved++;

            Repaint();
        }
    }
}
#endif
