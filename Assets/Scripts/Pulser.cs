using System;
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

        static Color _offColour;
        static Color _onColour;
        Material _mat;

        public uint State { get; private set; }
        public bool IsOn => State > 0;
        
        void Awake() {
            _mat      = GetComponent<MeshRenderer>().material;
            State    = 0u;
            _onColour  = Color.HSVToRGB(0f, 0.68f, 1f);
            _offColour = Color.HSVToRGB(0f, 0f, 0.72f);
            _mat.color = _offColour;
            
            
            // create and init pinreceptor child
            _child = Instantiate(prefab, transform, false);
            _child.transform.localPosition = new Vector3(0.5f, 0);
            _child.Initialise(this, 0, true);
            
            ProblemSpace.Instance.Register(this);
        }

        void OnDestroy() {
            ProblemSpace.Instance.Register(this);
        }

        public void Pulse() {
            _mat.color   =  State > 0u ? _onColour : _offColour;
            _child.State =  State;
            _child.Pulse();
        }

        public IReadOnlyList<IPulser> Neighbours() => PinReceptor.Neighbours(_child);

        void OnMouseDown() {
            State        ^= 1u;
            ProblemSpace.Instance.PushEvent(this);
        }
    }
}