using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace Fixor {
    public class GameManager : MonoBehaviour {
        bool gameOver = false;
        bool won = false;

        static GameManager _lilInstance;
        public static GameManager Instance {
            get {
                if (_lilInstance) return _lilInstance;

                GameObject singletonObject = new(nameof(GameManager));
                _lilInstance = singletonObject.AddComponent<GameManager>();
                return _lilInstance;
            }
        }

        void LateUpdate() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
            }
            
            if (Input.GetKeyDown(KeyCode.R)) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (won && Input.GetKeyDown(KeyCode.N)) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        
        public void GameOver() {
            gameOver = true;
            won = VerifySolution.CheckSolution();

            GameObject gameOverText = GameObject.FindGameObjectWithTag("GameOverText");
            TextMeshProUGUI text = gameOverText.GetComponent<TextMeshProUGUI>();
            gameOverText.transform.parent.GetComponent<GraphicRaycaster>().enabled = false;
            gameOverText.transform.localScale = Vector3.one;
            text.text = won ? "You Won!\npress (N) to go to next scene" : "you lost & blew up\npress (R) to restart";
        }
    }
}
