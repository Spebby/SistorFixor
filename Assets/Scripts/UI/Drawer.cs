using System.Collections;
using UnityEngine;


namespace Fixor {
    [RequireComponent(typeof(RectTransform))]
    public class Drawer : MonoBehaviour {
        [SerializeField] Vector2 closedPosition;
        [SerializeField] Vector2 openPosition;
        [SerializeField] AnimationCurve animationCurve;
        [SerializeField] float endTime;

        [SerializeField] Transform _cr;
        RectTransform _rt;
        
        bool _isOpen;
        void Awake() {
            _isOpen            = false;
            _rt                = GetComponent<RectTransform>();
            _rt.anchoredPosition = closedPosition;
        }

        public void ToggleDrawer() {
            StartCoroutine(Animate(_isOpen ? openPosition : closedPosition,
                                          _isOpen ? closedPosition : openPosition));
        }

        IEnumerator Animate(Vector2 start, Vector2 end) {
            float elapsedTime = 0;
            while (elapsedTime < endTime) {
                _rt.anchoredPosition = Vector3.Lerp(start, end, animationCurve.Evaluate(elapsedTime));
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
            }

            _isOpen = !_isOpen;
        }

        void OnDrawGizmos() {
            Gizmos.matrix = _cr.localToWorldMatrix;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(closedPosition, 10);
            Gizmos.DrawWireSphere(openPosition, 10);
            Gizmos.DrawLine(closedPosition, openPosition);
        }
    }
}