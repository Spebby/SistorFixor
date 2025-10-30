using UnityEngine;

namespace Fixor {
    public class PinReceptor : MonoBehaviour {
        bool _isOut = false;
        public uint Index { get; private set; }

        public void Initialise(bool isOut, uint index) {
            _isOut = isOut;
            Index = index;
        }

        public uint GetState = 1;
    }
}