using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace Fixor {
    public class Chip : Piece, IPulser {
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

        MeshRenderer _mesh;
        TextMeshPro _text;

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
            Name = name;
            ChipType = chipType;
            nimPins = nim;
            noutPins = nout;
           
            gameObject.name = Name;
            
            // determine rect size based on name width & pin count
            _text.text  = Name;
            // height is calculated from receptor size
            _receptors = new PinReceptor[nimPins + noutPins];
            _mesh.material.color = chipType switch {
                Type.NAND => Color.red,
                Type.NOT  => Color.HSVToRGB(250, 25, 50),
                Type.AND  => Color.HSVToRGB(0, 40, 100),
                Type.OR   => Color.green,
                _         => Color.gray
            };

            float receptorHeight;
            {
                PinReceptor p   = Instantiate(receptorAsset);
                receptorHeight  = p.GetComponentInChildren<MeshRenderer>().bounds.size.y;
                receptorHeight += receptorHeight * 0.1f;
                DestroyImmediate(p.gameObject);
            }

            float nodeHeight = receptorHeight * Mathf.Max(nimPins, noutPins);
            //_mesh.transform.localScale = new Vector3(Mathf.Abs(_text.flexibleWidth * _text.transform.lossyScale.x), nodeHeight, 1);
            
            for (uint i = 0; i < nimPins; ++i) {
                PinReceptor p       = Instantiate(receptorAsset, transform, false);
                float       vertPos = 0f;
                if (nimPins > 1) {
                    vertPos = i > 0 ? 0.5f : -0.5f;
                }
                p.transform.localPosition = new Vector3(-0.5f, vertPos);
                _receptors[i] = p;
                p.Initialise(this, i, false);
                // distribute in pins along size
            }

            for (uint i = 0; i < noutPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                p.transform.localPosition = new Vector3(0.5f, 0);
                _receptors[i + nimPins] = p;
                p.Initialise(this, i, true);
                // distribute out pins along size
            }
            
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