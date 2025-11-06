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

        // ^ consider replacing w/ addressable

        readonly GameObject[] _ioButtons = new GameObject[2];
        readonly GameObject[] _chipButtons = new GameObject[NAMES.Length];

        // needs to run during initialiser.
        public void Initialise() {
            _ioButtons[0] = InitButton("IN",  () => SpawnObj(ServiceLocator.PulserPrefab.gameObject));
            _ioButtons[1] = InitButton("OUT", () => SpawnObj(ServiceLocator.OutputPrefab.gameObject));

            // add the buttons (todo replace names with dynamic list)
            int i = 0;
            foreach (Chip.Type type in NAMES) {
                _chipButtons[i++] = InitButton(type.ToString(), () => {
                    GameObject chip = SpawnObj(ServiceLocator.ChipPrefab.gameObject);
                    chip.GetComponent<Chip>().Initialise(name: type.ToString(), chipType: type, nim: type == NOT ? 1u : 2u);
                });
            }
        }

        public void ToggleIOButtons() {
            foreach (GameObject button in _ioButtons) button.SetActive(!button.activeInHierarchy);
        }

        public void ToggleChipButtons() {
            foreach (GameObject button in _chipButtons) button.SetActive(!button.activeInHierarchy);
        }

        public void ToggleNonUniversalGates() {
            foreach (GameObject button in _chipButtons) {
                if (button.name is not ("Create (NAND)" or "Create (NOR)"))
                    button.SetActive(!button.activeInHierarchy);
            }
        }

        GameObject InitButton(in string name, Action spawnAction) {
            GameObject      obj  = Instantiate(ServiceLocator.ButtonPrefab.gameObject, ScrollBar);
            TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
            text.text = name;
            obj.name  = $"Create ({name})";
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => spawnAction());
            return obj;
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