using Hikari.Puzzle;
using UniRx;
using UnityEngine;

namespace Hikari.AI {
    public class HikariAIBehaviour : MonoBehaviour, IController {
        private HikariAI ai;
        private Game game;
        private void Awake() {
            ai = new HikariAI();
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