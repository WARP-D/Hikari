using System.Linq;
using Cyanite.Puzzle.UI;
using TMPro;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.VFX;

namespace Cyanite.Puzzle {
    public class GameView : MonoBehaviour {
        public Game game;

        [SerializeField] private Transform fieldOrigin;
        [SerializeField] private FallingPieceBehaviour[] queue;
        [SerializeField] private FallingPieceBehaviour holdPiece;
        [SerializeField] private FallingPieceBehaviour controllingPiece;
        [SerializeField] private FallingPieceBehaviour ghostPiece;
        [SerializeField] private DamageBar damageBar;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private CellBlock cellPrefab;
        [SerializeField] private VisualEffect lineClearEffect;
        [SerializeField] private AttackSummaryUI attackSummary;
        [SerializeField] private RenVisionUI renVisionPrefab;
        private Transform canvas;

        private readonly CellBlock[][] cells = new CellBlock[40][];

        [Header("Settings")] public bool showGhost = true;

        private readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private bool isInClearEffect;

        private void Awake() {
            for (var i = 0; i < cells.Length; i++) {
                cells[i] = new CellBlock[10];
            }

            controllingPiece.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            subscriptions.Dispose();
        }

        public void Init() {
            canvas = attackSummary.transform.parent;
            damageBar.Amount = 0;
            playerNameText.text = game.Player.Name;
            var controller = GetComponent<InputController>();
            if (controller) game.Controller = controller;
            game.EventStream
                .OfType<Game.IGameEvent, Game.QueueUpdatedEvent>()
                .Subscribe(e => {
                    for (var i = 0; i < queue.Length; i++) {
                        queue[i].piece = new Piece(game.Board.nextPieces.ElementAt(i), 0, 0, 0);
                        queue[i].MakeShapeAndColor();
                    }

                    UpdateCurrentPiece();
                });
            game.EventStream
                .OfType<Game.IGameEvent, Game.FallingPieceMovedEvent>()
                .Subscribe(e => UpdateCurrentPiece()).AddTo(subscriptions);
            game.EventStream
                .OfType<Game.IGameEvent, Game.GotDamageEvent>()
                .Subscribe(e => { damageBar.Amount = game.Damage; }).AddTo(subscriptions);
            game.EventStream
                .OfType<Game.IGameEvent, Game.GarbageLinesAddedEvent>()
                .Subscribe(async e => {
                    if (isInClearEffect) await UniTask.DelayFrame(40);
                    
                    var move = e.rows.Length;
                    for (var i = cells.Length - 1; i >= cells.Length - e.rows.Length; i--) {
                        foreach (var cellBlock in cells[i].Where(cb => cb)) {
                            Destroy(cellBlock.gameObject);
                        }
                    }

                    for (var i = cells.Length - e.rows.Length - 1; i >= 0; i--) {
                        cells[i + e.rows.Length] = cells[i];
                        foreach (var cellBlock in cells[i + e.rows.Length].Where(cb => cb)) {
                            cellBlock.transform.Translate(new Vector3(0, move, 0), Space.Self);
                        }
                    }

                    for (var i = 0; i < e.rows.Length; i++) {
                        cells[i] = new CellBlock[10];
                        for (var j = 0; j < 10; j++) {
                            var cellValue = e.rows[i][j];
                            if (cellValue != 0) {
                                var obj = Instantiate(cellPrefab.gameObject, fieldOrigin);
                                obj.transform.localPosition = new Vector3(j, i);
                                var block = obj.GetComponent<CellBlock>();
                                block.materialIndex = cellValue;
                                cells[i][j] = block;
                            }
                        }
                    }
                }).AddTo(subscriptions);
            game.EventStream
                .OfType<Game.IGameEvent, Game.PieceLockedEvent>()
                .Subscribe(async e => {
                    foreach (var position in e.piece.GetCellPositions()) {
                        if (cells[position.y][position.x] != null) continue;
                        var obj = Instantiate(cellPrefab.gameObject, fieldOrigin);
                        var block = obj.GetComponent<CellBlock>();
                        block.transform.localPosition = new Vector3(position.x, position.y);
                        block.materialIndex = (int) e.piece.kind;
                        block.UpdateMaterial();
                        cells[position.y][position.x] = block;
                    }

                    controllingPiece.gameObject.SetActive(false);
                    ghostPiece.gameObject.SetActive(false);

                    if (e.lockResult.clearedLines.Any()) {
                        isInClearEffect = true;
                        attackSummary.Show(e.lockResult.placementKind);
                        if (e.lockResult.ren > 1) {
                            Instantiate(renVisionPrefab, canvas).GetComponent<RenVisionUI>().renCount =
                                (int) (e.lockResult.ren - 1);
                        }

                        foreach (var line in e.lockResult.clearedLines) {
                            var obj = Instantiate(lineClearEffect.gameObject, fieldOrigin);
                            obj.transform.localPosition = new Vector3(4.5f, line, -0.5f);
                            var vfx = obj.GetComponent<VisualEffect>();
                            vfx.SetInt("Piece Color", (int) e.piece.kind);
                        }

                        await UniTask.DelayFrame(18);

                        foreach (var line in e.lockResult.clearedLines) {
                            for (var i = 0; i < cells[line].Length; i++) {
                                var cellBlock = cells[line][i];
                                Destroy(cellBlock.gameObject);
                                cells[line][i] = null;
                            }
                        }

                        await UniTask.DelayFrame(20);

                        var c = 0;
                        foreach (var line in e.lockResult.clearedLines) {
                            for (var i = line - c; i < cells.Length - 1; i++) {
                                cells[i] = cells[i + 1];
                            }

                            c++;
                        }

                        for (var i = 0; i < e.lockResult.clearedLines.Count; i++) {
                            cells[cells.Length - 1 - i] = new CellBlock[10];
                        }

                        for (var y = 0; y < cells.Length; y++) {
                            for (var x = 0; x < 10; x++) {
                                if (cells[y][x]) cells[y][x].transform.localPosition = new Vector3(x, y);
                            }
                        }
                        
                        isInClearEffect = false;
                    }
                }).AddTo(subscriptions);
            game.EventStream
                .OfType<Game.IGameEvent, Game.HoldEvent>()
                .Subscribe(e => {
                    if (game.Board.holdPiece == null) {
                        holdPiece.gameObject.SetActive(false);
                    } else {
                        holdPiece.gameObject.SetActive(true);
                        holdPiece.piece = new Piece(game.Board.holdPiece.Value, 0, 0, 0);
                        holdPiece.MakeShapeAndColor();
                        UpdateCurrentPiece();
                    }
                }).AddTo(subscriptions);
        }

        private void UpdateCurrentPiece() {
            if (game.CurrentPiece.HasValue) {
                controllingPiece.gameObject.SetActive(true);
                controllingPiece.piece = game.CurrentPiece.Value;
                controllingPiece.MakeShapeAndColor();
                controllingPiece.UpdatePosition();
            } else {
                controllingPiece.gameObject.SetActive(false);
            }

            if (showGhost && game.Ghost.HasValue) {
                ghostPiece.gameObject.SetActive(true);
                ghostPiece.piece = game.Ghost.Value;
                ghostPiece.MakeShapeAndColor();
                ghostPiece.UpdatePosition();
            } else {
                ghostPiece.gameObject.SetActive(false);
            }
        }
    }
}