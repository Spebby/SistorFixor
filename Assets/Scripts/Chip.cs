using System;
using UnityEngine;


namespace Fixor {
    public class Chip : MonoBehaviour, IOperation {
        public enum Type {
            NOT,
            AND,
            OR,
            XOR,
            NAND,
            NOR,
            CUSTOM
        }

        public string Name { get; private set; } = "Untitled";
        public Type ChipType { get; private set; }
        
        public uint InPins  { get; private set; }
        public uint OutPins { get; private set; }

        public uint nimPins  = 2;
        public uint noutPins = 1;

        

        [SerializeField] PinReceptor receptorAsset;
        PinReceptor[] _receptors;

        void Start() {
            // determine size
            _receptors = new PinReceptor[nimPins + noutPins];
            
            for (uint i = 0; i < nimPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                _receptors[i] = p;
                p.Initialise(false, i);
                // distribute in pins along size
            }

            for (uint i = 0; i < noutPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                _receptors[i + nimPins] = p;
                p.Initialise(true, i);
                // distribute out pins along size
            }
            
            // if custom chip, we then have to figure out its internal logic
        }

        void Pulse() {
            InPins = 0;
            for (int i = 0; i < nimPins; ++i) {
                InPins |= _receptors[i].GetState << i; 
            }

            OutPins = Operate(InPins);
        }

        public uint Operate(uint input) {
            throw NotImplementedException;
        }

        internal Span<Chip> Neighbours() {
            throw NotImplementedException;  
        }
    }

    // Todo make readonly
    [System.Serializable]
    public class Wire : MonoBehaviour {
        [SerializeField] PinReceptor A;
        [SerializeField] PinReceptor B;

        public void Initialise(in string a, in string b) {
            A = ProblemSpace.Instance.Pins[a];
            B = ProblemSpace.Instance.Pins[b];
        }
        
        // Pulsing should ONLY EVER be called through ProblemSpace.
        public void Pulse() => B.SetState(A.GetState);

        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(A.transform.position, B.transform.position);
        }
    }
}