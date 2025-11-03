using System.Collections.Generic;
using UnityEngine;


namespace Fixor {
    public class ProblemSpace : MonoBehaviour {
        public HashSet<Chip> chips = new();
        public HashSet<Wire> wires = new();

        [SerializeField] PinReceptor[] ins;
        [SerializeField] PinReceptor[] outs;
        
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
            Application.targetFrameRate = 60;
        }
        
        public void Register(Chip chip) {
            chips.Add(chip);
            foreach (Wire w in Chip.OutWires(chip)) {
                wires.Add(w);
            }
            
            // copy contents of event queues to new queues.
        }

        public void Deregister(Chip chip) {
            chips.Remove(chip);
            // dont manually remove wires since destructor on wires will handle it
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
            int steps = chips.Count + wires.Count;
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