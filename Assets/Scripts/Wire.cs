using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Fixor {
    public class Wire : MonoBehaviour {
        [SerializeField] internal PinReceptor A;
        [SerializeField] internal PinReceptor B;

        LineRenderer _lr;
        
        void OnDestroy() {
            A.wires.Remove(this);
            B.wires.Remove(this);
            ProblemSpace.Instance.wires.Remove(this);
        }

        public void Initialise(PinReceptor a, PinReceptor b) {
            A = a;
            B = b;
            
            A.AddWire(this, true);
            B.AddWire(this, false);
            
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.material      = new Material(Shader.Find("Sprites/Default"));
            _lr.startColor    = _lr.endColor = Color.cyan;
            _lr.startWidth    = _lr.endWidth = 0.2f;
            _lr.positionCount = 2;
            _lr.SetPosition(0, A.transform.position);
            _lr.SetPosition(1, B.transform.position);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pulse() => B.State = A.State;

        void LateUpdate() {
            _lr?.SetPosition(0, A.transform.position);
            _lr?.SetPosition(1, B.transform.position);
        }

        void OnMouseUpAsButton() {
            Destroy(this);
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
                if (!StartPin.IsOut) {
                    if (!endPin.IsOut) {
                        CancelDrag();
                        return;
                    }
                    (StartPin, endPin) = (endPin, StartPin);
                    // if wire is "backwards" then flip.
                }
                
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