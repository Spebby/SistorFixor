using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Fixor {
    public class Wire : MonoBehaviour {
        [SerializeField] internal PinReceptor A;
        [SerializeField] internal PinReceptor B;

        LineRenderer _lr;
        EdgeCollider2D _edge;

        static Color _onColour;
        static Color _offColour;
        
        void OnDestroy() {
            A.outWires.Remove(this);
            A.wires.Remove(this);
            B.wires.Remove(this);
            B.State = 0u;
            ProblemSpace.Instance.Deregister(this);
        }

        public void Initialise(PinReceptor a, PinReceptor b) {
            _onColour  = Color.HSVToRGB(0f, 0.68f, 0.85f);
            _offColour = Color.HSVToRGB(0f, 0f, 0.15f);
            
            A               = a;
            B               = b;
            gameObject.name = $"Wire_{A.name}/{B.name}";
            
            A.AddWire(this, true);
            B.AddWire(this, false);
            
            _lr               = gameObject.AddComponent<LineRenderer>();
            _lr.material      = new Material(Shader.Find("Sprites/Default")) { color = A.State > 0 ? _onColour : _offColour };
            _lr.startColor    = _lr.endColor = Color.white;
            _lr.startWidth    = _lr.endWidth = 0.2f;
            _lr.positionCount = 2;
            _lr.SetPosition(0, A.transform.position);
            _lr.SetPosition(1, B.transform.position);


            gameObject.AddComponent<EdgeCollider2D>();
            _edge                  = gameObject.GetComponent<EdgeCollider2D>();
            _edge.edgeRadius       = _lr.endWidth * 0.5f;

            Pulse();
            ProblemSpace.Instance.Register(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() {
            B.State = A.State;
            _lr.material.color = B.State > 0 ? _onColour : _offColour;
        }

        void LateUpdate() {
            if (!_lr) return;
            List<Vector2> points = new() { A.transform.position, B.transform.position };
            _lr?.SetPosition(0, points[0]);
            _lr?.SetPosition(1, points[1]);
            _edge?.SetPoints(points);
        }

        void OnMouseUpAsButton() {
            Destroy(gameObject);
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
            _line.startWidth    = 0.2f;
            _line.endWidth      = 0.2f;
            _line.positionCount = 2;
        }

        public static void UpdateDrag(Vector3 worldPosition) {
            if (!IsDragging || !_line) return;
            _line.SetPosition(0, StartPin.transform.position);
            _line.SetPosition(1, worldPosition);
        }

        public static void EndDrag(PinReceptor endPin) {
            if (!IsDragging) {
                CancelDrag();
                return;
            }
        
            if (endPin && endPin != StartPin) {
                if (StartPin.IsOut == endPin.IsOut) {
                    CancelDrag();
                    return;
                }

                // If wires are "backwards" flip their order
                if (!StartPin.IsOut) (StartPin, endPin) = (endPin, StartPin);

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