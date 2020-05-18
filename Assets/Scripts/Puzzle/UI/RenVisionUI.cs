using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cyanite.Puzzle.UI {
    public class RenVisionUI : MonoBehaviour {
        [SerializeField] private Image bg0;
        [SerializeField] private Image bg1;

        [SerializeField] private TMP_Text renValue;
        [SerializeField] private TMP_Text label;

        private Sequence seq;

        public int renCount;

        private float displayTime = 1.2f;

        private void Start() {
            renValue.text = renCount.ToString();
            bg0.fillAmount = 0;
            bg1.fillAmount = 0;
            seq = DOTween.Sequence();
            seq.Append(transform.DOLocalMoveX(-100,.6f).From(true).SetEase(Ease.OutCirc))
                .Join(bg0.DOFillAmount(1f, .5f).SetEase(Ease.OutCirc))
                .Insert(.15f, bg1.DOFillAmount(1f, .5f).SetEase(Ease.OutCirc))
                .Join(renValue.DOFade(0, .5f).From())
                .Join(label.DOFade(0, .5f).From())
                .AppendCallback(() => {
                    bg0.fillOrigin = 1;
                    bg1.fillOrigin = 1;
                })
                .Insert(displayTime + .1f,transform.DOLocalMoveX(100,.5f).SetRelative(true).SetEase(Ease.InCirc))
                .Insert(displayTime, bg1.DOFillAmount(0, .5f).SetEase(Ease.InCirc))
                .Insert(displayTime + .1f, bg0.DOFillAmount(0, .5f).SetEase(Ease.InCirc))
                .Join(renValue.DOFade(0, .5f))
                .Join(label.DOFade(0, .5f));
        }
    }
}