using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace Fixor {
    // Larger asset that can be clicked to pulse loops
    public interface IPulser {
        void Pulse();
        IReadOnlyList<IPulser> Neighbours();
    }

    
    public class Pulser : Piece, IPulser {
        internal PinReceptor Child;

        static Color _offColour;
        static Color _onColour;
        Material _mat;
        TextMeshPro _text;

        public uint State { get; private set; }
        
        void Awake() {
            name       = $"Pulser[{(char)('A' + ProblemSpace.Instance.NumInputs)}]";
            _text      = GetComponentInChildren<TextMeshPro>();
            _text.text = $"{(char)('A' + ProblemSpace.Instance.NumInputs)}";
            
            _mat       = GetComponent<MeshRenderer>().material;
            State      = 0u;
            _onColour  = Color.HSVToRGB(0f, 0.68f, 1f);
            _offColour = Color.HSVToRGB(0f, 0f, 0.72f);
            _mat.color = _offColour;
            
            
            // create and init receptor child
            Child = Instantiate(ServiceLocator.ReceptorPrefab, transform, false);
            Child.transform.localPosition = new Vector3(0.5f, 0, -1);
            Child.Initialise(this, 0, true);

            ProblemSpace.Instance.Register(this);
        }

        void OnDestroy() {
            ProblemSpace.Instance.Deregister(this);
        }

        public void Pulse() {
            _mat.color   =  State > 0u ? _onColour : _offColour;
            Child.State =  State;
            Child.Pulse();
        }

        public IReadOnlyList<IPulser> Neighbours() => PinReceptor.Neighbours(Child);

        void OnMouseDown() {
            State        ^= 1u;
            ProblemSpace.Instance.PushEvent(this);
        }
    }
}