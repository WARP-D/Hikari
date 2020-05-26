using DG.Tweening;
using UnityEngine;

namespace Hikari.Puzzle {
    public class DamageBar : MonoBehaviour {
        private Tween tween;

        public uint Amount {
            set {
                tween?.Complete();
                if (value == 0) {
                    tween = transform.DOScaleY(value, 0.2f).SetEase(Ease.OutQuint)
                        .OnComplete(() => gameObject.SetActive(false));
                } else {
                    gameObject.SetActive(true);
                    tween = transform.DOScaleY(value, 0.2f).SetEase(Ease.OutQuint);
                }
            }
        }
    }
}