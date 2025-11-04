using System.Collections.Generic;
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
                return _lilInstance;
            }
        }

        // The register functions *ALWAYS* pulse on new relevant connections
        public void Register(Chip chip) => _chips.Add(chip);
        public void Register(Pulser pulser) => _ins.Add(pulser);
        public void Register(Output o) => _outs.Add(o);
        public void Register(Wire wire) {
            _wires.Add(wire);
            foreach (Pulser p in _ins) {
                _currQueue.Enqueue(p);
            }
            _currQueue.Enqueue(wire.B.Parent);
        }

        public void Deregister(Chip chip) => _chips.Remove(chip);
        public void Deregister(Pulser pulser) => _ins.Remove(pulser);
        public void Deregister(Output o) => _outs.Remove(o);
        public void Deregister(Wire wire) {
            _wires.Remove(wire);
            foreach (Pulser p in _ins) {
                _currQueue.Enqueue(p);
            }
            _currQueue.Enqueue(wire.B.Parent);
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

        
        // Prime example of code to replace post-prototype
        public void InitLevel(GraphDataSO level) {
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
            
            LayoutNodes(inputNodes, chipNodes, outputNodes);
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
                float x = bottomLeft.x + width * 0.1f; // 10% from left
                float y = topRight.y - inputSpacing * (i + 1);
                inputs[i].transform.position = new Vector3(x, y, Z);
            }

            // Outputs on right
            for (int i = 0; i < nOutputs; i++) {
                float x = topRight.x - width * 0.1f; // 10% from right
                float y = topRight.y - outputSpacing * (i + 1);
                outputs[i].transform.position = new Vector3(x, y, Z);
            }

            // Chips in between
            if (nChips <= 0) return;
            
            float chipSpacingX = width / (nChips + 1);
            for (int i = 0; i < nChips; i++) {
                float x = bottomLeft.x + chipSpacingX * (i + 1);
                float y = topRight.y - chipSpacingY * (i + 1);
                chips[i].transform.position = new Vector3(x, y, Z);
            }
        }

        
        
        [SerializeField] GameObject ChipPrefab;
        [SerializeField] GameObject PulserPrefab;
        [SerializeField] GameObject OutputPrefab;
        public Pulser CreateInput() {
            Pulser p = Instantiate(PulserPrefab).GetComponent<Pulser>();
            return p;
        }

        public Output CreateOutput() {
            Output o = Instantiate(OutputPrefab).GetComponent<Output>();
            return o;
        }
        
        public Chip CreateChip(Chip.Type type, string name = "") {
            Chip c = Instantiate(ChipPrefab).GetComponent<Chip>();
            c.Initialise(string.IsNullOrEmpty(name) ? type.ToString() : name , type, type == Chip.Type.NOT ? 1u : 2u, 1u);
            return c;
        }
        
        public static void CreateWire(PinReceptor a, PinReceptor b) {
            GameObject obj = new(nameof(Wire));
            Wire       w   = obj.AddComponent<Wire>();
            w.Initialise(a, b);
        }
    }
}