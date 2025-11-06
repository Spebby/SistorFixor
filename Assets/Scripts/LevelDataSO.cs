using UnityEngine;

namespace Fixor {
    [CreateAssetMenu(fileName = "NewLevel", menuName = "SO/LevelDataSO")]
    public class LevelDataSO : ScriptableObject {
        public GraphDataSO solution;
        public GraphDataSO provided;

        public bool allowIOSpawning    = true;
        public bool allowChipSpawning  = true;
        public bool universalGatesOnly = false;
        
        
        [Header("Timer")]
        public float timerLength = 0; // if 0, disabled
        public bool shouldAnimate = false;
    }
}
