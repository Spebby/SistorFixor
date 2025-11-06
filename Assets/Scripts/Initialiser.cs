using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Fixor {
    public class Initialiser : MonoBehaviour {
        [Header("Global Config")]
        [SerializeField] PinReceptor PinReceptorPrefab;
        [SerializeField] Chip ChipPrefab;
        [SerializeField] Pulser PulserPrefab;
        [SerializeField] Output OutputPrefab;
        [SerializeField] Button ButtonPrefab;
        
        [Header("Level Config")]
        [SerializeField] LevelDataSO levelData;

        VerifySolution _vs;
        ChipSpawner _cs;
        TextMeshProUGUI _tTableText;
        
        // For some reason if this is Start, not Awake then ProblemSpace loses reference to its prefab references
        void Awake() {
            ServiceLocator.Initialiser(PinReceptorPrefab, ChipPrefab, PulserPrefab, OutputPrefab, ButtonPrefab, levelData);
           
            _ = GameManager.Instance;
            ProblemSpace.Instance.InitLevel(levelData.provided);
            string table = TruthTableGenerator.GraphToString(levelData.solution);
            Debug.Log(table);
            
            _tTableText = GameObject.FindGameObjectWithTag("TruthTableText").GetComponent<TextMeshProUGUI>();
            _tTableText.text = table;
           
            GameObject uiPanel = GameObject.FindGameObjectWithTag("ChipBar");
            _cs = uiPanel.GetComponent<ChipSpawner>();
            _vs = uiPanel.GetComponent<VerifySolution>();
            
            _vs.Initialise();
            _cs.Initialise();
            if (!levelData.allowIOSpawning) {
                _cs.ToggleIOButtons();
            }
            if (!levelData.allowChipSpawning) {
                _cs.ToggleChipButtons();
            }
            if (levelData.universalGatesOnly) {
                _cs.ToggleNonUniversalGates();
            }
            
            GraphDataSO data = ProblemSpace.Instance.Serialise();
            data.OnBeforeSerialize();
            
            Timer timer             = FindAnyObjectByType<Timer>();
            timer.GetComponent<Drawer>().enabled = levelData.shouldAnimate;
            if (timer) timer.Notify += GameManager.Instance.GameOver;
        }
    }
}
