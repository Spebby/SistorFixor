using TMPro;
using UnityEngine;

namespace Fixor {
    public class Initialiser : MonoBehaviour {
        [SerializeField] LevelDataSO levelData;
        
        ChipSpawner _cs;
        TextMeshProUGUI _ttableText;
        
        // For some reason if this is Start, not Awake then ProblemSpace loses reference to its prefab references
        void Awake() {
            _ = GameManager.Instance;
            _ = ProblemSpace.Instance;
            ProblemSpace.Instance.InitLevel(levelData.provided);
            string table = TruthTableGenerator.GraphToString(levelData.solution);
            Debug.Log(table);
            
            _ttableText = GameObject.FindGameObjectWithTag("TruthTableText").GetComponent<TextMeshProUGUI>();
            _ttableText.text = table;
            
            _cs = GameObject.FindGameObjectWithTag("ChipBar").GetComponent<ChipSpawner>();
            _cs.Initialise();
            if (!levelData.allowIOSpawning) {
                _cs.ToggleIOButtons();
            }
            if (!levelData.allowChipSpawning) {
                _cs.ToggleChipButtons();
            }
            
            GraphDataSO data = ProblemSpace.Instance.Serialise();
            data.OnBeforeSerialize();
        }
    }
}
