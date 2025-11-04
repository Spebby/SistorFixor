using UnityEngine;

namespace Fixor {
    public class Initialiser : MonoBehaviour {
        [SerializeField] GraphDataSO graphData;
        void Awake() {
            _ = GameManager.Instance;
            _ = ProblemSpace.Instance;
            ProblemSpace.Instance.InitLevel(graphData);
        }
    }
}
