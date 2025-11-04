using System.Collections.Generic;
using UnityEngine;


namespace Fixor {
    public class ProblemSpace : MonoBehaviour {
        public HashSet<Chip> chips = new();
        public HashSet<Wire> wires = new();

        HashSet<Pulser> ins = new();
        HashSet<Output> outs = new();
        
        // We have to evaluate things *IN STEP* and in order to get correct output.
        // Additionally, account for cyclic states.

        protected static ProblemSpace LilInstance;
        public static ProblemSpace Instance {
            get {
                if (LilInstance) return LilInstance;
                
                GameObject singletonObject = new(nameof(ProblemSpace));
                LilInstance            = singletonObject.AddComponent<ProblemSpace>();
                LilInstance._currQueue = LilInstance._queueA;
                LilInstance._nextQueue = LilInstance._queueB;
                return LilInstance;
            }
        }

        void Awake() {
            Application.targetFrameRate = 120;
        }

        // The register functions *ALWAYS* pulse on new relevant connections
        public void Register(Chip chip) => chips.Add(chip);
        public void Register(Pulser pulser) => ins.Add(pulser);
        public void Register(Output o) => outs.Add(o);
        public void Register(Wire wire) {
            wires.Add(wire);
            foreach (Pulser p in ins) {
                _currQueue.Enqueue(p);
            }
            _currQueue.Enqueue(wire.B.Parent);
        }

        public void Deregister(Chip chip) => chips.Remove(chip);
        public void Deregister(Pulser pulser) => ins.Remove(pulser);
        public void Deregister(Output o) => outs.Remove(o);
        public void Deregister(Wire wire) {
            wires.Remove(wire);
            foreach (Pulser p in ins) {
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
            int steps = chips.Count + wires.Count + ins.Count + outs.Count;
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
    }
}