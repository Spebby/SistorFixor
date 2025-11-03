using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Fixor.Chip.Type;

namespace Fixor {
    public class ChipSpawner : MonoBehaviour {
        static readonly Chip.Type[] NAMES = { NAND, NOT, AND, OR, XOR, NOR };
        
        [SerializeField] RectTransform ScrollBar;
        [SerializeField] GameObject ButtonPrefab;
        [SerializeField] GameObject ChipPrefab;
        [SerializeField] GameObject PulserPrefab;
        // ^ consider replacing w/ addressable

        void Awake() {
            {
                GameObject      button = Instantiate(ButtonPrefab.gameObject, ScrollBar);
                TextMeshProUGUI text   = button.GetComponentInChildren<TextMeshProUGUI>();
                text.text   = "IN";
                button.name = "Create (Pulser)";
                Button b = button.GetComponent<Button>();
                b.onClick.AddListener(() => {
                    Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f);
                    Vector3 worldCenter  = Camera.main!.ScreenToWorldPoint(screenCenter);
                    worldCenter.z = 0f;
                    Instantiate(PulserPrefab, worldCenter, Quaternion.identity);
                });
            }

            // add the buttons (atsp replace names with dynamic list)
            foreach (Chip.Type type in NAMES) {
                GameObject obj = Instantiate(ButtonPrefab.gameObject, ScrollBar);
                TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
                text.text = type.ToString();
                obj.name  = $"Create ({type.ToString()})";
                
                Button button = obj.GetComponent<Button>();
                button.onClick.AddListener(() => {
                    SpawnChip(type);
                });
            }
        }

        void SpawnChip(Chip.Type type) {
            // Convert from screen space → world space
            Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 worldCenter = Camera.main!.ScreenToWorldPoint(screenCenter);
            worldCenter.z = 0;
            
            GameObject obj = Instantiate(ChipPrefab.gameObject, worldCenter, Quaternion.identity);
            obj.GetComponent<Chip>().Initialise(name: type.ToString(), chipType: type, nim: type == NOT ? 1u : 2u);
            // TODO: either spawn w/ player holding OR spawn in middle of screen
        }
    }
}