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
    }
}