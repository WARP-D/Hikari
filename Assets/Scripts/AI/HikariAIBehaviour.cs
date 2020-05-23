using Hikari.Puzzle;
using TMPro;
using UniRx;
using UnityEngine;

namespace Hikari.AI {
    public class HikariAIBehaviour : MonoBehaviour, IController {
        private HikariAI ai;
        private Game game;

        [SerializeField] private TMP_Text length;
        
        private void Awake() {
            ai = new HikariAI();
        }

        private void Start() {
            game = GetComponent<GameView>().game;
            ai.Start();

            game.EventStream.OfType<Game.IGameEvent, Game.QueueUpdatedEvent>().Subscribe(e => {
                ai.AddNextPiece(e.kind);
            }).AddTo(this);
            game.EventStream.OfType<Game.IGameEvent, Game.PieceSpawnedEvent>().Subscribe(e => {
                ai.GetNextMove();
            }).AddTo(this);
        }

        private void Update() {
            ai.Update();
            length.text = ai.length.ToString();
        }

        private void OnDestroy() {
            ai.Dispose();
        }

        public Command RequestControlUpdate() {
            return 0; //TODO
        }
    }
}