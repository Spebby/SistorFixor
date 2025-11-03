using System.Collections.Generic;
using UnityEngine;


namespace Fixor {
    // Larger asset that can be clicked to pulse loops
    public interface IPulser {
        void Pulse();
        IReadOnlyList<IPulser> Neighbours();
    }

    
    public class Pulser : Piece, IPulser {
        [SerializeField] PinReceptor prefab;
        PinReceptor _child;
        uint _state;

        [SerializeField] Color offColor;
        [SerializeField] Color onColor;
        Material _mat;

        void Awake() {
            _mat = GetComponent<MeshRenderer>().material;
            _state = 0u;
            _mat.color = offColor;
            
            
            // create and init pinreceptor child
            _child = Instantiate(prefab, transform, false);
            _child.transform.localPosition = new Vector3(0.5f, 0);
            _child.Initialise(this, 0, true);
        }

        public void Pulse() {
            _mat.color   = _state > 0u ? onColor : offColor;
            _child.State = _state;
            _child.Pulse();
        }

        public IReadOnlyList<IPulser> Neighbours() => PinReceptor.Neighbours(_child);

        void OnMouseDown() {
            _state ^= 1u;
            ProblemSpace.Instance.PushEvent(this);
        }
    }
}