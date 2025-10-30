using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = UnityEngine.Object;


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


        [Header("Evil UI Hooks")]
        [SerializeField] TextMeshPro text;
        [SerializeField] GameObject body;

        const float receptorPadding = 2f;
        internal void Initialise(in string name, in Type chipType, uint nim = 2, uint nout = 1) {
            // Constructor stuff
            Name = name;
            ChipType = chipType;
            nimPins = nim;
            noutPins = nout;
            
            // determine rect size based on name width & pin count
            text.text  = Name;
            // height is calculated from receptor size
            _receptors = new PinReceptor[nimPins + noutPins];
            Renderer rend           = receptorAsset.GetComponent<Renderer>();
            float    receptorHeight = rend.bounds.size.y + receptorPadding;

            body.transform.localScale = new Vector3(text.preferredWidth * text.transform.lossyScale.x
                                                  , receptorHeight * (nimPins + noutPins)
                                                  , 1);
            
            for (uint i = 0; i < nimPins; ++i) {
                PinReceptor p = Instantiate(receptorAsset, transform, false);
                p.transform.localPosition = rend.bounds.center + (Vector3.left * rend.bounds.extents.x) 
                                                               + (Vector3.down * rend.bounds.extents.y + Vector3.up * receptorHeight * i);
                _receptors[i]             = p;
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
                neigh.Capacity += r.outWires.Capacity;
                neigh.AddRange(r.wires.Select(w => ProblemSpace.Instance.ChipToPin[w.B]));
            }

            return neigh.AsReadOnly();
        }
    }

    
    public class PinReceptor : MonoBehaviour {
        bool _isOut;
        public uint Index { get; private set; } // serialisation purposes
        [SerializeField] internal List<Wire> wires;
        [SerializeField] internal List<Wire> outWires; // these are the important ones
        
        public void Initialise(uint index, bool isOut = false) {
            Index  = index;
            _isOut = isOut;
        }

        public uint State { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() {
            foreach (Wire w in outWires) w.Pulse();
        }
        
        // 🖱 Mouse interaction
        void OnMouseDown() {
            // Start dragging wire
            WireDragController.BeginDrag(this);
        }

        void OnMouseDrag() {
            if (!WireDragController.IsDragging) return;

            // Convert mouse position to world space
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main!.WorldToScreenPoint(transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            WireDragController.UpdateDrag(worldPos);
        }

        void OnMouseUp() {
            // Detect if we’re releasing over another pin
            Ray ray = Camera.main!.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                PinReceptor endPin = hit.collider.GetComponent<PinReceptor>();
                WireDragController.EndDrag(endPin);
            } else {
                WireDragController.CancelDrag();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddWire(in Wire wire, bool isOut) {
            wires.Add(wire);
            if (!isOut) return;
            outWires.Add(wire);
        }

        public void RemoveWires() {
            if (_isOut) throw new Exception("RemoveWires should not be called on an out pin!");
            foreach (Wire wire in wires) Destroy(wire.gameObject);
        }
    }

    
    public class Wire : MonoBehaviour {
        [SerializeField] internal PinReceptor A;
        [SerializeField] internal PinReceptor B;

        void OnDestroy() {
            A.wires.Remove(this);
            B.wires.Remove(this);
        }

        public void Initialise(PinReceptor a, PinReceptor b) {
            A.AddWire(this, true);
            B.AddWire(this, false);
            
            LineRenderer lr = gameObject.AddComponent<LineRenderer>();
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.startColor    = lr.endColor = Color.cyan;
            lr.startWidth    = lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, A.transform.position);
            lr.SetPosition(1, B.transform.position);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() => B.State = A.State;

        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(A.transform.position, B.transform.position);
        }
    }
    
    public static class WireDragController {
        public static bool IsDragging { get; private set; }
        public static PinReceptor StartPin { get; private set; }
        static LineRenderer _line;

        public static void BeginDrag(PinReceptor startPin) {
            if (IsDragging) return;

            IsDragging = true;
            StartPin   = startPin;

            // Create a temporary line for visual feedback
            GameObject lineObj = new("WirePreview");
            _line               = lineObj.AddComponent<LineRenderer>();
            _line.material      = new Material(Shader.Find("Sprites/Default"));
            _line.startColor    = Color.black;
            _line.endColor      = Color.black;
            _line.startWidth    = 0.02f;
            _line.endWidth      = 0.02f;
            _line.positionCount = 2;
        }

        public static void UpdateDrag(Vector3 worldPosition) {
            if (!IsDragging || !_line) return;
            _line.SetPosition(0, StartPin.transform.position);
            _line.SetPosition(1, worldPosition);
        }

        public static void EndDrag(PinReceptor endPin) {
            if (!IsDragging) return;
        
            if (endPin && endPin != StartPin) {
                GameObject wireObj = new($"Wire_{StartPin.name}/{endPin.name}");
                Wire       wire    = wireObj.AddComponent<Wire>();
               
                // I don't want input pins to have multiple connections, outpins can have more than one.
                if (endPin.wires.Count > 0) endPin.RemoveWires();
                wire.Initialise(StartPin, endPin);
            }

            // Clean up
            Object.Destroy(_line.gameObject);
            _line      = null;
            IsDragging = false;
            StartPin   = null;
        }

        public static void CancelDrag() {
            if (_line) Object.Destroy(_line.gameObject);
            _line      = null;
            IsDragging = false;
            StartPin   = null;
        }
    }
}