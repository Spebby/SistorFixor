using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;


namespace Fixor {
    public class ProblemSpace : MonoBehaviour {
        readonly HashSet<Chip>  _chips = new();
        readonly HashSet<Wire>  _wires = new();
        readonly HashSet<Pulser>  _ins = new();
        readonly HashSet<Output> _outs = new();

        public int NumInputs => _ins.Count;
        public int NumOutputs => _outs.Count;
        
        // We have to evaluate things *IN STEP* and in order to get correct output.
        // Additionally, account for cyclic states.

        static ProblemSpace _lilInstance;
        public static ProblemSpace Instance {
            get {
                if (_lilInstance) return _lilInstance;
                
                GameObject singletonObject = new(nameof(ProblemSpace));
                _lilInstance            = singletonObject.AddComponent<ProblemSpace>();
                _lilInstance._currQueue = _lilInstance._queueA;
                _lilInstance._nextQueue = _lilInstance._queueB;
                DontDestroyOnLoad(singletonObject);
                
                return _lilInstance;
            }
        }

        void OnDestroy() {
            if (_lilInstance == this) _lilInstance = null;
        }

        void Reset() {
            _chips.Clear();
            _wires.Clear();
            _ins.Clear();
            _outs.Clear();
        }

        // The register functions *ALWAYS* pulse on new relevant connections
        public void Register(Chip chip) => _chips.Add(chip);
        public void Register(Pulser pulser) => _ins.Add(pulser);
        public void Register(Output o) => _outs.Add(o);
        public void Register(Wire wire) {
            _wires.Add(wire);
            PulseStart();
            _currQueue.Enqueue(wire.B.Parent);
        }

        public void Deregister(Chip chip) => _chips.Remove(chip);
        public void Deregister(Pulser pulser) => _ins.Remove(pulser);
        public void Deregister(Output o) => _outs.Remove(o);
        public void Deregister(Wire wire) {
            _wires.Remove(wire);
            PulseStart();
            _currQueue.Enqueue(wire.B.Parent);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PulseStart() {
            foreach (Pulser p in _ins) {
                _currQueue.Enqueue(p);
            }
        }


        Queue<IPulser> _currQueue = null;
        Queue<IPulser> _nextQueue = null;
        Queue<IPulser> _queueA = new();
        Queue<IPulser> _queueB = new();

        const float STEP_INTERVAL = 0.08f;
        float _lastStep;
        void FixedUpdate() {
            if (!(Time.time - _lastStep > STEP_INTERVAL)) return;
            _lastStep = Time.time;
            Tick();
        }
       
        // I assume chip's in-pins have already been updated from a wire pulse.
        void Tick() {
            int steps = _chips.Count + _wires.Count + _ins.Count + _outs.Count;
            for (int i = 0; i < steps; i++) {
                while (_currQueue.TryDequeue(out IPulser p)) {
                    if (p is null) continue; // can be null due to destruction
                    
                    p.Pulse();
                    IReadOnlyList<IPulser> neighbours = p.Neighbours();
                    foreach (IPulser n in neighbours) _nextQueue.Enqueue(n);
                }

                (_currQueue, _nextQueue) = (_nextQueue, _currQueue);
            }
        }

        /// <summary>
        /// Assumes the chip has not already been pulsed.
        /// </summary>
        /// <param name="pulser"></param>
        public void PushEvent(IPulser pulser) {
            _currQueue.Enqueue(pulser);
        }

        
        /**
         * Graph construction code for problems
         */
        
		// NOTE: Temporary post-prototype workaround
		// If a gate has fewer connected inputs than required (e.g. A -> NAND),
		// the simulator will duplicate the existing input across all remaining
		// unconnected input pins. This allows single-input connections like
		// A -> NAND(A, A) to function correctly without explicit multi-pin wiring.
		// If we had more complex chips this would be unacceptable, but it's acceptable
		// here
        public void InitLevel(GraphDataSO level) {
            Reset();
            int inputs  = level.inputCount;
            int chips   = level.chipCount;
            int outputs = level.outputCount;
            int n       = level.NodeCount;

            // Step 1: Create all nodes
            Pulser[] inputNodes  = new Pulser[inputs];
            Chip[]   chipNodes   = new Chip[chips];
            Output[] outputNodes = new Output[outputs];

            for (int i = 0; i < inputs; i++)  inputNodes[i]  = CreateInput();
            for (int i = 0; i < chips; i++) {
                Chip.Type type = level.chipTypes[i];
                int countBefore = 0;
                for (int j = 0; j < i; j++) {
                    if (level.chipTypes[j] == type) countBefore++;
                }

                chipNodes[i] = CreateChip(type, $"{type.ToString()}{countBefore}");
            }
            for (int i = 0; i < outputs; i++) outputNodes[i] = CreateOutput();

            // Step 2: Map matrix indices to actual node objects
            object[] nodeMap                                              = new object[n];
            for (int i = 0; i < inputs; i++)  nodeMap[i]                  = inputNodes[i];
            for (int i = 0; i < chips; i++)   nodeMap[inputs + i]         = chipNodes[i];
            for (int i = 0; i < outputs; i++) nodeMap[inputs + chips + i] = outputNodes[i];

            // Step 3: Connect wires based on adjacency matrix
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    if (!level.Matrix[i, j]) continue; // no connection

                    PinReceptor sourceReceptor;
                    PinReceptor targetReceptor;

                    // Determine source receptor
                    if (i < inputs) sourceReceptor = inputNodes[i].Child; // assuming Pulser has outputPin
                    else {
                        bool aIsChip = i < inputs + chips;
                        if (aIsChip) sourceReceptor = chipNodes[i - inputs].Output;
                        else continue; // outputs don't send signals
                    }

                    // Determine target receptor
                    if (j < inputs) continue; // inputs don't receive
                    
                    // if we are connecting to a chip...
                    bool bIsChip = j < inputs + chips;
                    targetReceptor = bIsChip ? chipNodes[j - inputs].GetNextFreeInput() :
                        outputNodes[j - inputs - chips].Child; // if we are connecting to an output

                    if (sourceReceptor && targetReceptor)
                        CreateWire(sourceReceptor, targetReceptor);
                }
            }

            // Fill dangling chip input pins with the previous input's connection
            // (since adjacency matrix cannot encode duplicate A→A connections)
            foreach (Chip chip in chipNodes) {
                PinReceptor r = chip.GetNextFreeInput();
                if (r == null) continue;
                uint        i = r.Index;
                if (i is > 2 or 0) continue;
                
                // dis' ass but we ball.
                CreateWire(r, chip.FirstIn.wires[0].A);
            }
            
            LayoutNodes(inputNodes, chipNodes, outputNodes);
            PulseStart();
        }

        static void LayoutNodes(Pulser[] inputs, Chip[] chips, Output[] outputs) {
            Camera      cam        = Camera.main;
            const float Z          = 0f; // or distance from camera
            Vector3     bottomLeft = cam!.ViewportToWorldPoint(new Vector3(0, 0, Z));
            Vector3     topRight   = cam!.ViewportToWorldPoint(new Vector3(1, 1, Z));

            float width  = topRight.x - bottomLeft.x;
            float height = topRight.y - bottomLeft.y;

            int nInputs  = inputs.Length;
            int nOutputs = outputs.Length;
            int nChips   = chips.Length;

            float inputSpacing  = height / (nInputs + 1);
            float outputSpacing = height / (nOutputs + 1);
            float chipSpacingY  = height / (nChips + 1);

            // Inputs on left
            for (int i = 0; i < nInputs; i++) {
                float x = bottomLeft.x + width * 0.075f; // 10% from left
                float y = topRight.y - inputSpacing * (i + 1);
                inputs[i].transform.position = new Vector3(x, y, Z);
            }

            // Outputs on right
            for (int i = 0; i < nOutputs; i++) {
                float x = topRight.x - width * 0.075f; // 10% from right
                float y = topRight.y - outputSpacing * (i + 1);
                outputs[i].transform.position = new Vector3(x, y, Z);
            }

            // Chips in between
            if (nChips <= 0) return;
            
            float chipSpacingX = width / (nChips + 1);
            for (int i = 0; i < nChips; i++) {
                float x = bottomLeft.x + chipSpacingX * (i + 1);
                float y = topRight.y - chipSpacingY * (i + 1) + height * 0.05f;
                chips[i].transform.position = new Vector3(x, y, Z);
            }
        }
        

        public static Pulser CreateInput() {
            Pulser p = Instantiate(ServiceLocator.PulserPrefab).GetComponent<Pulser>();
            return p;
        }

        public static Output CreateOutput() {
            Output o = Instantiate(ServiceLocator.OutputPrefab).GetComponent<Output>();
            return o;
        }
        
        public static Chip CreateChip(Chip.Type type, string chipName = "") {
            Chip c = Instantiate(ServiceLocator.ChipPrefab).GetComponent<Chip>();
            c.Initialise(string.IsNullOrEmpty(chipName) ? type.ToString() : chipName , type, type == Chip.Type.NOT ? 1u : 2u, 1u);
            return c;
        }
        
        public static void CreateWire(PinReceptor a, PinReceptor b) {
            GameObject obj = new(nameof(Wire));
            Wire       w   = obj.AddComponent<Wire>();
            w.Initialise(a, b);
        }

        public GraphDataSO Serialise() => GraphDataSO.BuildGraphData(_ins, _chips, _outs);
    }

    public static class TruthTableGenerator {
        // currently this doesn't report if the graph is invalid. I think that's an acceptable tradeoff.
        public static List<(bool[] inputs, bool[] outputs)> GenerateTruthTable(GraphDataSO graph) {
            int nInputs   = graph.inputCount;
            int nChips    = graph.chipCount;
            int nOutputs  = graph.outputCount;
            int nodeCount = graph.NodeCount;

            // Build adjacency list
            List<int>[] adj                            = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++) adj[i] = new List<int>();

            for (int i = 0; i < nodeCount; i++) {
                for (int j = 0; j < nodeCount; j++) {
                    if (graph.Matrix[i, j])
                        adj[i].Add(j);
                }
            }

            // Kahn's algorithm
            int[] inDegree = new int[nodeCount];
            for (int i = 0; i < nodeCount; i++) {
                foreach (int j in adj[i])
                    inDegree[j]++;
            }

            Queue<int> queue = new();
            for (int i = 0; i < nodeCount; i++) {
                if (inDegree[i] == 0)
                    queue.Enqueue(i);
            }

            List<int> topoOrder = new();
            while (queue.Count > 0) {
                int u = queue.Dequeue();
                topoOrder.Add(u);

                foreach (int v in adj[u]) {
                    inDegree[v]--;
                    if (inDegree[v] == 0) queue.Enqueue(v);
                }
            }
            // ^ this may actually get stuck if theres a cycle. idk tho

            if (topoOrder.Count != nodeCount) throw new InvalidOperationException("Graph contains a cycle!");

            // --- Generate truth table ---
            int                                   totalCombinations = 1 << nInputs;
            List<(bool[] inputs, bool[] outputs)> table             = new(totalCombinations);

            for (int combo = 0; combo < totalCombinations; combo++) {
                bool[] nodeValues = new bool[nodeCount];

                // Assign input bits
                for (int i = 0; i < nInputs; i++)
                    nodeValues[i] = ((combo >> i) & 1) == 1;

                // Evaluate nodes in topological order
                foreach (int nodeIndex in topoOrder) {
                    if (nodeIndex < nInputs || nodeIndex >= nInputs + nChips) continue; // inputs already set/is output
                    
                    // Chip nodes
                    int        chipIdx = nodeIndex - nInputs;
                    List<bool> inputs  = new();
                    for (int src = 0; src < nodeCount; src++) {
                        if (graph.Matrix[src, nodeIndex])
                            inputs.Add(nodeValues[src]);
                    }

                    // Fill dangling pins for multi-input chips
                    if (graph.chipTypes[chipIdx] != Chip.Type.NOT && inputs.Count > 0 && inputs.Count != 2) {
                        bool last = inputs[^1];
                        while (inputs.Count < 3) inputs.Add(last);
                    }

                    uint packed = PackInputs(inputs);
                    nodeValues[nodeIndex] = (graph.chipTypes[chipIdx] switch {
                        Chip.Type.AND    => Operations.AND(packed),
                        Chip.Type.OR     => Operations.OR(packed),
                        Chip.Type.NOT    => Operations.NOT(packed),
                        Chip.Type.XOR    => Operations.XOR(packed),
                        Chip.Type.NAND   => Operations.NAND(packed),
                        Chip.Type.NOR    => Operations.NOR(packed),
                        Chip.Type.CUSTOM => throw new NotImplementedException("Custom chips not currently supported"),
                        _                => throw new ArgumentOutOfRangeException()
                    } & 0b1) != 0;
                }

                // Collect outputs (done outside above loop for code simplicity)
                bool[] outputs = new bool[nOutputs];
                for (int outIdx = 0; outIdx < nOutputs; outIdx++) {
                    int        nodeIndex = nInputs + nChips + outIdx;
                    List<bool> inputs    = new();
                    for (int src = 0; src < nodeCount; src++) {
                        if (graph.Matrix[src, nodeIndex])
                            inputs.Add(nodeValues[src]);
                    }

                    outputs[outIdx] = inputs.Exists(v => v);
                }

                table.Add((GetInputBits(combo, nInputs), outputs));
            }

            return table;
        }


        static uint PackInputs(List<bool> inputs) {
            switch (inputs.Count) {
                case 0:
                    return 0;
                case 1:
                    return inputs[0] ? 1u : 0u;
            }

            // NOTE: Bit 0 (LSB) is the *left* input, Bit 1 is the *right* input
            uint a = inputs[0] ? 1u : 0u; // left
            uint b = inputs[1] ? 1u : 0u; // right
            return (b << 1) | a;
        }

        static bool[] GetInputBits(int combo, int nInputs) {
            bool[] bits = new bool[nInputs];
            for (int i = 0; i < nInputs; i++)
                bits[i] = ((combo >> i) & 1) == 1;
            return bits;
        }
        
        public static string GraphToString(GraphDataSO graph) {
            List<(bool[] inputs, bool[] outputs)> table = GenerateTruthTable(graph);
            int    nInputs  = graph.inputCount;
            int    nOutputs = graph.outputCount;

            StringBuilder sb = new();

            // --- Header ---
            for (int i = 0; i < nInputs; i++) {
                sb.Append((char)('A' + i));
                if (i < nInputs - 1) sb.Append(' ');
            }
            sb.Append(" | ");

            for (int i = 0; i < nOutputs; i++) {
                sb.Append($"O{(char)('A' + i)}");
                if (i < nOutputs - 1) sb.Append(' ');
            }
            sb.AppendLine();

            // --- Rows ---
            foreach ((bool[] inputs, bool[] outputs) in table) {
                for (int i = 0; i < inputs.Length; i++) {
                    sb.Append(inputs[i] ? 'T' : 'F');
                    if (i < inputs.Length - 1) sb.Append(' ');
                }

                sb.Append(" |  ");

                for (int i = 0; i < outputs.Length; i++) {
                    sb.Append(outputs[i] ? 'T' : 'F');
                    if (i < outputs.Length - 1) sb.Append("  ");
                    // two wide gap to account for output style (O0, O1)
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
