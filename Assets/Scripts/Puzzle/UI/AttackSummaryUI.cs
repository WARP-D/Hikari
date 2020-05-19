using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Hikari.Puzzle.UI {
    public class AttackSummaryUI : MonoBehaviour {
        [SerializeField] private TMP_Text text;
        private RectTransform textTr;
        [SerializeField] private RectTransform background;
        private Sequence sequence;

        private void Start() {
            textTr = (RectTransform) text.transform;
        }

        public void Show(PlacementKind kind) {
            sequence?.Complete();
            text.text = kind.GetFullName();
            sequence = DOTween.Sequence()
                .Append(background.DOAnchorPos(Vector2.zero, .18f))
                .Insert(.1f,textTr.DOAnchorPos(Vector2.zero, .15f).SetEase(Ease.OutBack))
                .Insert(.9f,textTr.DOAnchorPos(new Vector2(0,-80), .2f))
                .Join(background.DOAnchorPos(new Vector2(0,-80), .2f));
            sequence.Play();
        }
    }
}