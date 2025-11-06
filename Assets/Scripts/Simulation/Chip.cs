using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace Fixor {
    public class Chip : Piece, IPulser {
        public enum Type : byte {
            NOT,
            AND,
            OR,
            XOR,
            NAND,
            NOR,
            CUSTOM
        }

        public Type ChipType { get; private set; }

        /// <summary>Bitmask state of input pins</summary>
        public uint InPins { get; private set; } = uint.MaxValue; // default so pulse always runs on init
        /// <summary>Bitmask state of output pins</summary>
        public uint OutPins { get; private set; }

        public uint nimPins = 2;
        public uint noutPins = 1;


        PinReceptor[] _receptors;

        MeshRenderer _mesh;
        TextMeshPro _text;

        // Temp so I don't lose my mind
        internal PinReceptor Output => _receptors[^1];
        internal PinReceptor FirstIn => _receptors[0];
        
        // these aren't intended to be used outside ProblemSpace graph and should not be depended on
        // to be accurate.
        internal uint _inPinAllocated;
        internal PinReceptor GetNextFreeInput() {
            uint freeMask = ~_inPinAllocated & ((1u << (int)nimPins) - 1);
            if (freeMask == 0) return null; // all pins allocated

            int index = TrailingZeroCount(freeMask);
            _inPinAllocated |= 1u << index; // mark as allocated
            return _receptors[index];
        }

        // https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.Private.CoreLib/src/System/Numerics/BitOperations.cs#L602
        static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => new byte[] {
            00, 01, 28, 02, 29, 14, 24, 03,
            30, 22, 20, 15, 25, 17, 04, 08,
            31, 27, 13, 23, 21, 19, 16, 07,
            26, 12, 18, 06, 11, 05, 10, 09
        };

        // evil shit
        static int TrailingZeroCount(uint v) {
            return v == 0 ? 32 : // special case
                TrailingZeroCountDeBruijn[(int)(((v & (uint)-(int)v) * 0x077CB531u) >> 27)];
        }
        

        void Awake() {
            _mesh = GetComponent<MeshRenderer>();
            _text = GetComponentInChildren<TextMeshPro>();
        }

        void OnDestroy() {
            ProblemSpace.Instance.Deregister(this);
            foreach (PinReceptor r in _receptors) {
                Destroy(r);
            }
        }

        internal void Initialise(in string name, in Type chipType, uint nim = 2, uint nout = 1) {
            // Constructor stuff
            ChipType        = chipType;
            nimPins         = nim;
            noutPins        = nout;
           
            gameObject.name = name;
            _text.text      = chipType == Type.CUSTOM ? name : chipType.ToString();
            
            // determine rect size based on name width & pin count
            // height is calculated from receptor size
            _receptors = new PinReceptor[nimPins + noutPins];
            _mesh.material.color = chipType switch {
                Type.NAND => Color.red,
                Type.NOT  => Color.HSVToRGB(250, 25, 50),
                Type.AND  => Color.HSVToRGB(0, 40, 100),
                Type.OR   => Color.green,
                _         => Color.gray
            };

            /*
            float receptorHeight;
            {
                PinReceptor p   = Instantiate(ServiceLocator.ReceptorPrefab);
                receptorHeight  = p.GetComponentInChildren<MeshRenderer>().bounds.size.y;
                receptorHeight += receptorHeight * 0.1f;
                DestroyImmediate(p.gameObject);
            }
            */

            //float nodeHeight = receptorHeight * Mathf.Max(nimPins, noutPins);
            //_mesh.transform.localScale = new Vector3(Mathf.Abs(_text.flexibleWidth * _text.transform.lossyScale.x), nodeHeight, 1);
            
            for (uint i = 0; i < nimPins; ++i) {
                PinReceptor p       = Instantiate(ServiceLocator.ReceptorPrefab, transform, false);
                float       vertPos = 0f;
                if (nimPins > 1) {
                    vertPos = i > 0 ? -0.5f : 0.5f;
                }
                p.transform.localPosition = new Vector3(-0.5f, vertPos, -1);
                _receptors[i] = p;
                p.Initialise(this, i);
                // distribute in pins along size
            }

            for (uint i = 0; i < noutPins; ++i) {
                PinReceptor p = Instantiate(ServiceLocator.ReceptorPrefab, transform, false);
                p.transform.localPosition = new Vector3(0.5f, 0, -1);
                _receptors[i + nimPins] = p;
                p.Initialise(this, i, true);
                // distribute out pins along size
            }
            
            // Pulse self to initialise "default" output
            Pulse();
            
            // if custom chip, we then have to figure out its internal logic
            ProblemSpace.Instance.Register(this);
        }

        public void Pulse() {
            uint org = InPins;
            InPins = 0;
            for (int i = 0; i < nimPins; ++i) {
                InPins |= _receptors[i].State << i; 
            }

            // this may be causing some headaches. double check if this is actually the problem
            if (InPins == org) return;
            
            // pulse wires
            OutPins = Operate(InPins);
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
        
        public IReadOnlyList<IPulser> Neighbours() {
            List<IPulser> neigh = new();
            
            for (uint i = nimPins; i < noutPins + nimPins; ++i) {
                // get all wires.
                PinReceptor r = _receptors[i];
                neigh.Capacity += r.outWires.Capacity;
                neigh.AddRange(r.wires.Select(w => w.B.Parent));
            }

            return neigh.AsReadOnly();
        }

        internal static IReadOnlyList<Wire> OutWires(Chip c) {
            List<Wire> wires = new();
            for (uint i = c.nimPins; i < c.noutPins + c.nimPins; ++i) {
                PinReceptor r = c._receptors[i];
                wires.Capacity += r.outWires.Capacity;
                wires.AddRange(r.wires);
            }
            return wires.AsReadOnly();
        } 
    }
}