using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Fixor {
    public class PinReceptor : MonoBehaviour {
        protected internal bool IsOut { get; private set; }
        protected internal uint Index { get; private set; } // serialisation purposes
        // unused
        protected internal IPulser Parent { get; private set; }
        
        [SerializeField] internal List<Wire> wires;
        [SerializeField] internal List<Wire> outWires; // these are the important ones

        public void Initialise(IPulser parent, uint index, bool isOut = false) {
            Parent = parent;
            Index  = index;
            IsOut  = isOut;
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
            mousePos.z = 0;
            Vector3 worldPos = Camera.main!.ScreenToWorldPoint(mousePos);

            WireDragController.UpdateDrag(worldPos);
        }

        void OnMouseUp() {
            // Detect if we’re releasing over another pin
            Vector2 point = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);
            if (hit) {
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
            if (IsOut) throw new Exception("RemoveWires should not be called on an out pin!");
            foreach (Wire wire in wires) Destroy(wire.gameObject);
        }
        
        internal static IReadOnlyList<IPulser> Neighbours(PinReceptor p) {
            List<IPulser> neigh = new(p.outWires.Capacity);
            neigh.AddRange(p.wires.Select(w => w.B.Parent));
            return neigh.AsReadOnly();
        }
    }
} 