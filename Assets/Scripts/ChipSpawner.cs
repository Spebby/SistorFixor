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
        // ^ consider replacing w/ addressable

        void Awake() {
            // add the buttons (at some point this should be done procedurally but this is fine atm)

            foreach (Chip.Type type in NAMES) {
                GameObject obj = Instantiate(ButtonPrefab.gameObject, ScrollBar);
                TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
                text.text = type.ToString();
                
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
            
            GameObject obj = Instantiate(ChipPrefab.gameObject, worldCenter, Quaternion.identity);
            obj.GetComponent<Chip>().Initialise(type.ToString(), type);
            // TODO: either spawn w/ player holding OR spawn in middle of screen
        }
    }
}