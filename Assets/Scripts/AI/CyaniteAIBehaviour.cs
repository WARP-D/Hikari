using Cyanite.Puzzle;
using UniRx;
using UnityEngine;

namespace Cyanite.AI {
    public class CyaniteAIBehaviour : MonoBehaviour, IController {
        private CyaniteAI ai;
        private Game game;
        private void Awake() {
            ai = new CyaniteAI();
        }

        private void Start() {
            game = GetComponent<GameView>().game;
            ai.Start();

            game.EventStream.OfType<Game.IGameEvent, Game.QueueUpdatedEvent>().Subscribe(e => {
                ai.AddNextPiece(e.kind);
                Debug.Log(e.kind);
            }).AddTo(this);
        }

        private void Update() {
            ai.Update();
        }

        private void OnDestroy() {
            ai.Dispose();
        }

        public Command RequestControlUpdate() {
            return 0; //TODO
        }
    }
}