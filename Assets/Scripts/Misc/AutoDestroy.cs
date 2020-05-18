using UnityEngine;

namespace Cyanite.Misc {
    public class AutoDestroy : MonoBehaviour {
        [SerializeField] private float destroyDelay;

        private void Start() {
            Invoke(nameof(DestroySelf),destroyDelay);
        }

        private void DestroySelf() {
            Destroy(gameObject);
        }
    }
}