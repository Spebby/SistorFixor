using UnityEngine;

namespace Fixor {
    public class GameManager : MonoBehaviour {
        static GameManager _lilInstance;
        public static GameManager Instance {
            get {
                if (_lilInstance) return _lilInstance;

                GameObject singletonObject = new(nameof(GameManager));
                _lilInstance = singletonObject.AddComponent<GameManager>();
                return _lilInstance;
            }
        }
        
        
    }
}
