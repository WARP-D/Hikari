using System.Collections.Generic;
using System.Linq;
using Hikari.Puzzle;
using TMPro;
using UniRx;
using UnityEngine;

namespace Hikari.AI {
    public class HikariAIBehaviour : MonoBehaviour, IController {
        private HikariAI ai;
        private Game game;

        private bool nextMoveRequested;
        private Queue<Instruction> instructions = new Queue<Instruction>();
        private bool manipulating;
        private bool waitingForSonicDrop;
        private int hypertap;

        [SerializeField] private TMP_Text length;
        [SerializeField] private FallingPieceBehaviour preview;

        private void Awake() {
            ai = new HikariAI();
        }

        private void Start() {
            game = GetComponent<GameView>().game;
            ai.Start();
            preview.gameObject.SetActive(false);

            game.EventStream.OfType<Game.IGameEvent, Game.QueueUpdatedEvent>().Subscribe(e => {
                ai.AddNextPiece(e.kind);
            }).AddTo(this);
            game.EventStream.OfType<Game.IGameEvent, Game.PieceSpawnedEvent>().Subscribe(async e => {
                ai.RequestNextMove();
                nextMoveRequested = true;
            }).AddTo(this);
        }

        private void Update() {
            ai.Update();
            length.text = ai.length.ToString();
            if (nextMoveRequested) {
                if (ai.PollNextMove(out var move)) {
                    nextMoveRequested = false;
                    manipulating = true;
                    instructions.Clear();
                    if (move.HasValue) {
                        preview.gameObject.SetActive(true);
                        preview.piece = move.Value.piece;
                        preview.MakeShapeAndColor();
                        preview.UpdatePosition();
                        for (var i = 0; i < move.Value.length; i++) {
                            instructions.Enqueue(move.Value.GetInstructionAt(i));
                        }
                    }
                }
            }
        }

        private void OnDestroy() {
            ai.Dispose();
        }

        public Command RequestControlUpdate() {
            if (!manipulating) return 0;
            
            if (waitingForSonicDrop) {
                if (game.IsCurrentPieceGrounded) waitingForSonicDrop = false;
            }
            
            if (hypertap++ % 2 != 0) {
                return waitingForSonicDrop ? Command.SoftDrop : 0;
            }

            Command cmd = 0;

            if (waitingForSonicDrop) {
                cmd |= Command.SoftDrop;
            } else {
                if (instructions.Any()) {
                    switch (instructions.Dequeue()) {
                        case Instruction.Left:
                            cmd |= Command.Left;
                            break;
                        case Instruction.Right:
                            cmd |= Command.Right;
                            break;
                        case Instruction.Cw:
                            cmd |= Command.RotateRight;
                            break;
                        case Instruction.Ccw:
                            cmd |= Command.RotateLeft;
                            break;
                        case Instruction.SonicDrop:
                            waitingForSonicDrop = true;
                            cmd |= Command.SoftDrop;
                            break;
                    }
                } else {
                    cmd |= Command.HardDrop;
                    hypertap = 0;
                    waitingForSonicDrop = false;
                    manipulating = false;
                    preview.gameObject.SetActive(false);
                }
            }

            return cmd;
        }
    }
}