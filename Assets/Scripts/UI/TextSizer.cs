using TMPro;
using UnityEngine;
using static UnityEngine.RectTransform.Axis;


namespace Fixor {
    [RequireComponent(typeof(RectTransform))]
    public class TextSizer : MonoBehaviour {
        RectTransform _rect;
        TextMeshProUGUI _text;

        void Awake() {
            _rect = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        void LateUpdate() {
            _rect.SetSizeWithCurrentAnchors(Horizontal, _text.preferredWidth);
        }
    }
}
