using System;
using System.Collections.Generic;
using UnityEngine;


namespace Fixor {
    public class Output : Piece, IPulser {
        [SerializeField] GameObject prefab;
        PinReceptor _child;

        static Color _offColour;
        static Color _onColour;
        Material _mat;
        public uint State { get; private set; }
        public bool IsOn => State > 0;
        
        void Awake() {
            _mat       = GetComponent<MeshRenderer>().material;
            _onColour   = Color.HSVToRGB(0f, 0.68f, 1f);
            _offColour  = Color.HSVToRGB(0f, 0f, 0.72f);
            
            _mat.color = _offColour;
            
            _child                         = Instantiate(prefab, transform, true).GetComponent<PinReceptor>();
            _child.transform.localPosition = new Vector3(-0.5f, 0);
            _child.Initialise(this, 0);
            
            ProblemSpace.Instance.Register(this);
        }

        void OnDestroy() {
            ProblemSpace.Instance.Deregister(this);
        }

        public void Pulse() {
            State      = _child.State;
            _mat.color = IsOn ? _onColour : _offColour;
        }
        
        public IReadOnlyList<IPulser> Neighbours() {
            return new List<IPulser>().AsReadOnly();
            // return empty list
        }
    }
}