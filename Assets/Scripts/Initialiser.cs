using UnityEngine;

namespace Fixor {
    public class Initialiser : MonoBehaviour {
        void Awake() {
            ProblemSpace _ = ProblemSpace.Instance;
        }
    }
}
