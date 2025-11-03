using UnityEngine;


namespace Fixor {
    public abstract class Piece : MonoBehaviour {
        void OnMouseDrag() {
            transform.position = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }
    }
}