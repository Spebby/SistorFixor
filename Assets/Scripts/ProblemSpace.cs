using System;
using System.Collections.Generic;
using UnityEngine;


namespace Fixor {
    public class ProblemSpace : MonoBehaviour {
        [SerializeField] List<Chip> chips;
        [SerializeField] List<Wire> wires;

        [SerializeField] PinReceptor[] ins;
        [SerializeField] PinReceptor[] outs;
        
        // We have to evaluate things *IN STEP* and in order to get correct output.
        // Additionally, account for cyclic states.
        
        public readonly Dictionary<string, PinReceptor> Pins = new();

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
        
        public static void Register() {
            
            // copy contents of event queues to new queues.
        }


        Queue<Chip> _currQueue = null;
        Queue<Chip> _nextQueue = null;
        Queue<Chip> _queueA = new();
        Queue<Chip> _queueB = new();

        void FixedUpdate() {
            Tick(Time.deltaTime);
        }
        
        void Tick(float dt) {
            int steps = chips.Count + wires.Count;
            for (int i = 0; i < steps; i++) {
                while (_currQueue.TryDequeue(out Chip c)) {
                    Span<Chip> n = c.Neighbours();
                    foreach (Chip t in n) _nextQueue.Enqueue(t);
                }

                (_currQueue, _nextQueue) = (_nextQueue, _currQueue);
            }

            // swap queue target
        }
    }
}