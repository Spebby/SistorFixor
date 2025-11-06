using UnityEngine;
using UnityEngine.SceneManagement;


namespace Fixor {
    public class KillGame : MonoBehaviour {
        void Update() {
            if (Input.GetKeyDown(KeyCode.R)) {
                SceneManager.LoadScene(0);
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
            }
        }
    }
}
