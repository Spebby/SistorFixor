using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        void Initialise(in string name, in Type chipType, uint nim = 2, uint nout = 1) {
            // Constructor stuff
            Name = name;
            ChipType = chipType;
            nimPins = nim;
            noutPins = nout;
            
            // determine rect size based on name width & pin count
            _receptors = new PinReceptor[nimPins + noutPins];
            
            
            for (uint i = 0; i < nimPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                _receptors[i] = p;
                p.Initialise(i, false);
                // distribute in pins along size
            }

            for (uint i = 0; i < noutPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                _receptors[i + nimPins] = p;
                p.Initialise(i, true);
                // distribute out pins along size
            }
            
            // if custom chip, we then have to figure out its internal logic
        }

        public void Pulse() {
            InPins = 0;
            for (int i = 0; i < nimPins; ++i) {
                InPins |= _receptors[i].State << i; 
            }

            OutPins = Operate(InPins);
            
            // pulse wires
            for (int i = 0; i < noutPins; ++i) {
                PinReceptor r = _receptors[i + nimPins];
                r.State = (OutPins << i) & 1;
                r.Pulse();
            }
        }

        public uint Operate(uint input) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return ChipType switch {
                Type.NOT  => Operations.NOT(input),
                Type.XOR  => Operations.XOR(input),
                Type.AND  => Operations.AND(input),
                Type.OR   => Operations.OR(input),
                Type.NAND => Operations.NAND(input),
                Type.NOR  => Operations.NOR(input),
                _         => throw new NotImplementedException()
            };
        }

        internal IReadOnlyList<Chip> Neighbours() {
            List<Chip> neigh = new();
            // the dream of one day stackallocing this...
            
            for (uint i = nimPins; i < noutPins + nimPins; ++i) {
                // get all wires.
                PinReceptor r = _receptors[i];
                neigh.Capacity += r.wires.Length;
                neigh.AddRange(r.wires.Select(w => ProblemSpace.Instance.ChipToPin[w.B]));
            }

            return neigh.AsReadOnly();
        }
    }

    
    public class PinReceptor : MonoBehaviour {
        bool _isOut;                            // for rendering purposes
        public uint Index { get; private set; } // serialisation purposes
        [SerializeField] internal Wire[] wires;
        
        
        public void Initialise(uint index, bool isOut = false) {
            Index  = index;
            _isOut = isOut;
        }

        public uint State { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() {
            foreach (Wire w in wires) {
                w.Pulse();
            }
        }
    }

    
    // Todo make readonly
    [System.Serializable]
    public class Wire : MonoBehaviour {
        [SerializeField] internal PinReceptor A;
        [SerializeField] internal PinReceptor B;

        public void Initialise(in string a, in string b) {
            A = ProblemSpace.Instance.Pins[a];
            B = ProblemSpace.Instance.Pins[b];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() => B.State = A.State;

        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(A.transform.position, B.transform.position);
        }
    }
}