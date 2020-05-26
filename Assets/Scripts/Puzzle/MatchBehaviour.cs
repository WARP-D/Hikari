using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Hikari.Puzzle {
    public class MatchBehaviour : MonoBehaviour {
        private Match match;
        [SerializeField] private GameView gameView1;
        [SerializeField] private GameView gameView2;
        private IDisposable countdownSubscription;

        [SerializeField] private List<TMP_Text> countdownTexts;

        private void Awake() {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref playerLoop);
        }

        private void Start() {
            match = new Match();
            gameView1.game = match.game1;
            gameView2.game = match.game2;
            gameView1.Init();
            gameView2.Init();
            countdownTexts.ForEach(t => t.text = "");
            countdownSubscription = match.EventStream.OfType<object, Match.CountdownEvent>().Subscribe(e => {
                if (e.count > 0) {
                    countdownTexts.ForEach(t => t.text = e.count.ToString());
                } else if (e.count == 0) {
                    countdownTexts.ForEach(t => t.text = "GO");
                    Observable.TimerFrame(60).Subscribe(l => countdownTexts.ForEach(t => t.text = "")).AddTo(this);
                }
            });
        }

        private void OnDestroy() {
            match.Dispose();
        }
    }
}