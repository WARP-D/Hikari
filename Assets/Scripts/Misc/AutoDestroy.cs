using UnityEngine;

namespace Hikari.Misc {
    public class AutoDestroy : MonoBehaviour {
        [SerializeField] private float destroyDelay;

        private void Start() {
            Invoke(nameof(DestroySelf), destroyDelay);
        }

        private void DestroySelf() {
            Destroy(gameObject);
        }
    }
}