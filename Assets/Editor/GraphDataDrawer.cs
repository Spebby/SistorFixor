using UnityEngine;
using UnityEditor;


// an LLM generated this
namespace Fixor.Editor {
    [CustomEditor(typeof(GraphDataSO))]
    public class GraphDataEditor : UnityEditor.Editor {
        GraphDataSO _graph;

        void OnEnable() {
            _graph = (GraphDataSO)target;
            _graph.InitMatrix();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            // Counts
            int newInputs  = EditorGUILayout.IntField("Inputs", _graph.inputCount);
            int newChips   = EditorGUILayout.IntField("Chips", _graph.chipCount);
            int newOutputs = EditorGUILayout.IntField("Outputs", _graph.outputCount);

            if (newInputs != _graph.inputCount || newChips != _graph.chipCount || newOutputs != _graph.outputCount) {
                Undo.RecordObject(_graph, "Resize Graph");
                _graph.inputCount  = newInputs;
                _graph.chipCount   = newChips;
                _graph.outputCount = newOutputs;
                _graph.InitMatrix();
            }

            // Chip type selection
            if (_graph.chipCount > 0) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Chip Types", EditorStyles.boldLabel);
                for (int i = 0; i < _graph.chipCount; i++) {
                    _graph.chipTypes[i] = (Chip.Type)EditorGUILayout.EnumPopup($"C{i}", _graph.chipTypes[i]);
                }
            }

            EditorGUILayout.Space();

            // Draw matrix with labels
            int n = _graph.NodeCount;
            if (n == 0) {
                EditorGUILayout.HelpBox("No nodes to display.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Adjacency Matrix", EditorStyles.boldLabel);

            // Column headers
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            for (int j = 0; j < n; j++)
                GUILayout.Label(GetLabel(j, _graph), GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();

            // Rows
            for (int i = 0; i < n; i++) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(GetLabel(i, _graph), GUILayout.Width(60));
                for (int j = 0; j < n; j++) {
                    bool oldVal = _graph.Matrix[i, j];
                    bool newVal = EditorGUILayout.Toggle(oldVal, GUILayout.Width(25));
                    if (newVal == oldVal) continue;
                    Undo.RecordObject(_graph, "Edit Edge");
                    _graph.Matrix[i, j] = newVal;
                }

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
                EditorUtility.SetDirty(_graph);
        }

        static string GetLabel(int index, GraphDataSO g) {
            int inputs = g.inputCount;
            int chips  = g.chipCount;

            if (index < inputs) return $"{(char)('A' + index)}";
            
            if (index < inputs + chips) {
                int chipIndex = index - inputs;
                // fallback
                if (g.chipTypes.Count <= chipIndex) return $"C{chipIndex}";

                Chip.Type type = g.chipTypes[chipIndex];

                // Count how many chips of this type came before
                int countBefore = 0;
                for (int i = 0; i < chipIndex; i++) {
                    if (g.chipTypes[i] == type) countBefore++;
                }

                // e.g. AND0
                return $"{type}{countBefore}";
            }
            
            int outputIndex = index - inputs - chips;
            return $"O{outputIndex}";
        }
    }
}