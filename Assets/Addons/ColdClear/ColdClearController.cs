using System;
using System.Linq;
using Hikari.Puzzle;
using UniRx;
using UnityEngine;

namespace Hikari.Addons.ColdClear {
    public class ColdClearController : MonoBehaviour, IController {
        private Game game;
        private ColdClearBot bot;
        private bool isInputCapable;

        private void Start() {
            game = GetComponent<Game>();
            game.EventStream.OfType<Game.IGameEvent, Game.QueueUpdatedEvent>()
                .Subscribe(e => { bot.AddNextPiece(game.Board.nextPieces.LastOrDefault()); })
                .AddTo(this);
        }

        public Command RequestControlUpdate() {
            return 0;
        }
    }
}