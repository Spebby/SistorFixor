using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace Fixor {
    public class VerifySolution : MonoBehaviour {
        [SerializeField] RectTransform ScrollBar;
        
        public void Initialise() {
            GameObject      obj  = Instantiate(ServiceLocator.ButtonPrefab.gameObject, ScrollBar);
            TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
            obj.name  = "Verify";
            text.text = "VERIFY";
            
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => {
                bool passed = CheckSolution();
                Debug.Log(passed);
                if (passed) {
                    GameManager.Instance.GameOver();
                }
            });
        }

        // this prolly dangerous but i dont care
        public static bool CheckSolution() {
            GraphDataSO runtime = ProblemSpace.Instance.Serialise();
            
            // this sucks but makes my life easier
            string rTruth = TruthTableGenerator.GraphToString(runtime);
            string sTruth = TruthTableGenerator.GraphToString(ServiceLocator.LevelData.solution);
            
            #if UNITY_EDITOR
            AssetDatabase.CreateAsset(runtime, "Assets/DebugGraph.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            #endif
            
            Debug.Log(rTruth);
            return string.Equals(rTruth, sTruth);
        }
    }
}