using System.Collections;
using UnityEngine;


namespace Fixor {
    [RequireComponent(typeof(RectTransform))]
    public class Drawer : MonoBehaviour {
        [SerializeField] Vector2 closedPosition;
        [SerializeField] Vector2 openPosition;
        [SerializeField] AnimationCurve animationCurve;
        [SerializeField] float endTime;

        CanvasGroup _cg;
        RectTransform _rt;
        bool _isAnimating;
        
        bool _isOpen;
        void Awake() {
            _isOpen              = false;
            _cg                  = GetComponent<CanvasGroup>();
            _rt                  = GetComponent<RectTransform>();
            _rt.anchoredPosition = closedPosition;
        }

        public void ToggleDrawer() {
            if (_isAnimating) return;
            StartCoroutine(Animate(_isOpen ? openPosition : closedPosition,
                                          _isOpen ? closedPosition : openPosition));
        }

        IEnumerator Animate(Vector2 start, Vector2 end) {
            _isAnimating       = true;
            if (_cg) {
                _cg.blocksRaycasts = false;
                _cg.interactable   = false;
            }

            float elapsedTime = 0;
            while (elapsedTime < endTime) {
                _rt.anchoredPosition = Vector3.Lerp(start, end, animationCurve.Evaluate(elapsedTime));
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
            }

            _isAnimating = false;
            _isOpen      = !_isOpen;
            if (!_cg) yield break;
            _cg.blocksRaycasts = true;
            _cg.interactable   = true;
        }

        public void SetOpen() {
            _isOpen = true;
            _rt.anchoredPosition = openPosition;
        }

        public void SetClosed() {
            _isOpen = false;
            _rt.anchoredPosition = closedPosition;
        }

        void OnDrawGizmos() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(closedPosition, 10);
            Gizmos.DrawWireSphere(openPosition, 10);
            Gizmos.DrawLine(closedPosition, openPosition);
        }
    }
}