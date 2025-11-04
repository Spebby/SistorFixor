using UnityEngine;
using System;
using System.Collections.Generic;
using static Fixor.Chip.Type;


namespace Fixor {
    [CreateAssetMenu(fileName = "NewGraph", menuName = "Graphs/Adjacency Matrix Graph")]
    public class GraphDataSO : ScriptableObject, ISerializationCallbackReceiver {
        [Header("Node Counts")]
        public int inputCount = 2;
        public int chipCount = 3;
        public int outputCount = 1;

        [Header("Chip Configuration")]
        public List<Chip.Type> chipTypes = new();

        [NonSerialized] public bool[,] Matrix;

        [SerializeField, HideInInspector] bool[] serializedMatrix;
        [SerializeField, HideInInspector] int serializedSize;

        public int NodeCount => inputCount + chipCount + outputCount;

        public void InitMatrix() {
            int n = NodeCount;
            if (Matrix == null || Matrix.GetLength(0) != n)
                Matrix = new bool[n, n];

            // Ensure chipTypes list matches chipCount
            while (chipTypes.Count < chipCount)
                chipTypes.Add(AND);
            while (chipTypes.Count > chipCount)
                chipTypes.RemoveAt(chipTypes.Count - 1);
        }

        public void OnBeforeSerialize() {
            int n = NodeCount;
            serializedSize   = n;
            serializedMatrix = new bool[n * n];
            if (Matrix == null) return;
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++)
                    serializedMatrix[i * n + j] = Matrix[i, j];
            }
        }

        public void OnAfterDeserialize() {
            int n = serializedSize;
            Matrix = new bool[n, n];
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++)
                    Matrix[i, j] = serializedMatrix != null && serializedMatrix[i * n + j];
            }
        }

        /// <summary>
        /// Builds a GraphDataSO from runtime circuit objects.
        /// <i>(Intended for use in problem space)</i>
        /// </summary>
        public static GraphDataSO BuildGraphData(
            IEnumerable<Pulser> inputs,
            IEnumerable<Chip> chips,
            IEnumerable<Output> outputs) {
            GraphDataSO graph = CreateInstance<GraphDataSO>();
            List<Pulser> inputList  = new(inputs);
            List<Chip>   chipList   = new(chips);
            List<Output> outputList = new(outputs);

            graph.inputCount  = inputList.Count;
            graph.chipCount   = chipList.Count;
            graph.outputCount = outputList.Count;
            graph.InitMatrix();

            // --- Assign chip types ---
            graph.chipTypes.Clear();
            foreach (Chip chip in chipList)
                graph.chipTypes.Add(chip.ChipType);

            // --- Map objects to matrix indices ---
            Dictionary<object, int> nodeIndex = new(inputList.Count + chipList.Count + outputList.Count);

            for (int i = 0; i < inputList.Count; i++) nodeIndex[inputList[i]] = i;
            for (int i = 0; i < chipList.Count; i++) nodeIndex[chipList[i]] = graph.inputCount + i;
            for (int i = 0; i < outputList.Count; i++) nodeIndex[outputList[i]] = graph.inputCount + graph.chipCount + i;

            // --- Build connections ---
            MarkOutputs(inputList); // Inputs -> (Chips | Outputs)
            MarkOutputs(chipList);  // Chips → (Chips | Outputs)

            // Outputs have no outgoing edges
            return graph;

            void MarkOutputs<T>(List<T> list) where T : IPulser {
                foreach (T input in list) {
                    IReadOnlyList<IPulser> neigh = input.Neighbours();
                    if (neigh is null) continue;
                    int from = nodeIndex[input];
                    foreach (IPulser target in neigh) {
                        if (nodeIndex.TryGetValue(target, out int to))
                            graph.Matrix[from, to] = true;
                    }
                }
            }
        }
    }
}