using System;
using System.Runtime.CompilerServices;
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
        [SerializeField] GameObject OutputPrefab;
        // ^ consider replacing w/ addressable

        void Awake() {
            InitButton("IN",  () => SpawnObj(PulserPrefab));
            InitButton("OUT", () => SpawnObj(OutputPrefab));

            // add the buttons (atsp replace names with dynamic list)
            foreach (Chip.Type type in NAMES) {
                InitButton(type.ToString(), () => {
                    GameObject chip = SpawnObj(ChipPrefab);
                    chip.GetComponent<Chip>().Initialise(name: type.ToString(), chipType: type, nim: type == NOT ? 1u : 2u);
                });
            }
        }

        void InitButton(in string name, Action spawnAction) {
            GameObject      obj  = Instantiate(ButtonPrefab.gameObject, ScrollBar);
            TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
            text.text = name;
            obj.name  = $"Create ({name})";
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => spawnAction());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static GameObject SpawnObj(GameObject prefab) {
            // Convert from screen space → world space
            Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 worldCenter  = Camera.main!.ScreenToWorldPoint(screenCenter);
            worldCenter.z = 0;
            
            return Instantiate(prefab, worldCenter, Quaternion.identity);
        }
    }
}