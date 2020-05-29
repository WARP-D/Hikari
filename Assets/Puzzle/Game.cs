using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hikari.Puzzle {
    public class Game {
        public Match match;
        public PlayerInfo Player { get; private set; }
        public Board Board { get; private set; }
        public Piece? CurrentPiece { get; private set; }
        public Piece? Ghost { get; private set; }
        public uint Damage { get; private set; }
        public bool IsHoldLocked { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDead { get; private set; }

        public int Ren { get; private set; }
        public bool B2B { get; private set; }

        public IController Controller { get; set; }

        public float Gravity { get; } = 1f;
        private float yTimer = 0f;
        private float autoLockTimer = 0f;
        private int spawnDelay = -1;
        private uint attackValue = 0;
        private int attackDelay = -1;

        public IObservable<IGameEvent> EventStream;
        private readonly Subject<IGameEvent> eventSubject;
        private const float GarbageColumnRandomness = 0.3f;

        public Game(Match match, PlayerInfo player) {
            eventSubject = eventSubject = new Subject<IGameEvent>();
            EventStream = eventSubject.AsObservable();
            this.match = match;
            Player = player;
            Board = new Board(12);
            CurrentPiece = null;

            this.match.EventStream.OfType<Match.IMatchEvent, Match.CountdownEvent>()
                .Where(e => e.count == 3)
                .First()
                .Subscribe(e => {
                    foreach (var piece in Board.nextPieces) {
                        eventSubject.OnNext(new QueueUpdatedEvent(piece));
                    }
                    eventSubject.OnNext(new InitializedEvent());
                });
            this.match.EventStream.OfType<Match.IMatchEvent, Match.StartEvent>()
                .Subscribe(e => SpawnNewPiece());
            this.match.EventStream.OfType<Match.IMatchEvent, Match.UpdateEvent>()
                .Subscribe(e => Update());
        }

        private void SpawnNewPiece() {
            spawnDelay = -1;
            CurrentPiece = new Piece(Board.Next());
            if (Board.Collides(CurrentPiece.Value)) CurrentPiece = CurrentPiece.Value.WithOffset(Vector2Int.up);
            if (Board.Collides(CurrentPiece.Value)) {
                IsDead = true;
                Debug.Log($"Player{Player.ID} died");
                eventSubject.OnNext(DeathEvent.Default);
            }

            Ghost = Board.SonicDrop(CurrentPiece.Value);
            yTimer = 0f;
            autoLockTimer = 0f;
            eventSubject.OnNext(new PieceSpawnedEvent(CurrentPiece.Value.kind));
            eventSubject.OnNext(new QueueUpdatedEvent(Board.nextPieces.Last()));
            IsHoldLocked = false;
        }

        public void Update() {
            if (attackDelay == 0) {
                match.DistributeDamage(Player.ID, attackValue);
                attackValue = 0;
                attackDelay = -1;
            }

            if (attackDelay > 0) attackDelay--;

            if (spawnDelay == 0) SpawnNewPiece();

            if (spawnDelay > 0) {
                spawnDelay--;
                return;
            }

            if (IsDead) return;

            if (!CurrentPiece.HasValue) return;
            var currentPiece = CurrentPiece.Value;

            var updated = false;
            var gravityMultiplier = 1f;

            if (Controller != null) {
                var cmd = Controller.RequestControlUpdate();

                if ((cmd & Command.Hold) != 0) {
                    if (IsHoldLocked) return;
                    if (Board.holdPiece.HasValue) {
                        var tmp = Board.holdPiece.Value;
                        Board.holdPiece = currentPiece.kind;
                        CurrentPiece = new Piece(tmp);
                        Ghost = Board.SonicDrop(CurrentPiece.Value);
                    } else {
                        Board.holdPiece = currentPiece.kind;
                        SpawnNewPiece();
                    }

                    IsHoldLocked = true;
                    eventSubject.OnNext(HoldEvent.Default);

                    return;
                }

                if ((cmd & Command.RotateLeft) != 0) {
                    if (SRS.TryRotate(currentPiece, Board, false, out var result)) {
                        var spinStatus = Board.CheckTSpin(result.Item2, result.Item1);
                        currentPiece = result.Item2.WithTSpinStatus(spinStatus);
                        updated = true;
                    }
                }

                if ((cmd & Command.RotateRight) != 0) {
                    if (SRS.TryRotate(currentPiece, Board, true, out var result)) {
                        var spinStatus = Board.CheckTSpin(result.Item2, result.Item1);
                        currentPiece = result.Item2.WithTSpinStatus(spinStatus);
                        updated = true;
                    }
                }

                if ((cmd & Command.Left) != 0) {
                    currentPiece = currentPiece.WithOffset(Vector2Int.left);
                    if (Board.Collides(currentPiece)) {
                        currentPiece = currentPiece.WithOffset(Vector2Int.right);
                    } else {
                        updated = true;
                    }
                }

                if ((cmd & Command.Right) != 0) {
                    currentPiece = currentPiece.WithOffset(Vector2Int.right);
                    if (Board.Collides(currentPiece)) {
                        currentPiece = currentPiece.WithOffset(Vector2Int.left);
                    } else {
                        updated = true;
                    }
                }

                if ((cmd & Command.SoftDrop) != 0) gravityMultiplier = 20f;
                if ((cmd & Command.HardDrop) != 0) {
                    CurrentPiece = currentPiece;
                    LockPiece();
                }
            }

            Ghost = Board.SonicDrop(currentPiece);
            yTimer += Gravity * Time.deltaTime * gravityMultiplier;
            var fall = false;
            while (yTimer > 1) {
                if (currentPiece == Ghost.Value) {
                    yTimer = 0;
                    if (fall) updated = true;
                    break;
                }

                fall = true;
                currentPiece = currentPiece.WithOffset(Vector2Int.down);
                yTimer--;
                updated = true;
            }

            IsGrounded = currentPiece == Ghost.Value;

            if (updated) {
                CurrentPiece = currentPiece;
                eventSubject.OnNext(FallingPieceMovedEvent.Default);
            }
        }

        private void LockPiece() {
            var drop = Board.SonicDrop(CurrentPiece.Value);
            var lockResult = Board.Lock(drop);
            eventSubject.OnNext(new PieceLockedEvent(true, drop, lockResult));
            CurrentPiece = null;
            Ghost = null;
            if (lockResult.clearedLines.Any()) {
                var c = 0;
                foreach (var line in lockResult.clearedLines) {
                    for (var i = line - c; i < Board.row.Length - 1; i++) {
                        Board.row[i] = Board.row[i + 1];
                    }

                    c++;
                }

                for (var i = 0; i < lockResult.clearedLines.Count; i++) {
                    Board.row[Board.row.Length - i - 1] = new Board.Row();
                }

                spawnDelay = 18 + 20 + 3;
            } else {
                spawnDelay = 3;
            }

            var attack = lockResult.attack;

            if (lockResult.attack < Damage) {
                Damage -= lockResult.attack;
                AddGarbageLines(Damage);
            } else {
                attack -= Damage;
            }

            if (lockResult.attack > 0) {
                attackDelay = 20;
                attackValue = attack;
            }
        }

        public void AddDamage(uint attack) {
            Damage += attack;
            eventSubject.OnNext(new GotDamageEvent(attack));
        }

        private void AddGarbageLines(uint amount) {
            var columnPos = Random.Range(0, 10);
            var lines = new List<byte[]>();
            for (var i = 0; i < amount; i++) {
                if (Random.Range(0f, 1f) < GarbageColumnRandomness) {
                    columnPos = Random.Range(0, 10);
                }

                var line = new byte[10];
                for (var j = 0; j < line.Length; j++) {
                    line[j] = (byte) (columnPos == j ? 0 : 7);
                }

                lines.Add(line);
            }

            var array = lines.ToArray();
            Board.InsertRowsAtBottom(array);
            eventSubject.OnNext(new GarbageLinesAddedEvent(array));
        }

        public bool IsCurrentPieceGrounded {
            get {
                if (!CurrentPiece.HasValue || !Ghost.HasValue) return false;
                return CurrentPiece.Value == Ghost.Value;
            }
        }

        public static readonly int[] RenAttacks = {
            0, 0, // 0, 1 combo
            1, 1, // 2, 3 combo
            2, 2, // 4, 5 combo
            3, 3, // 6, 7 combo
            4, 4, 4, // 8, 9, 10 combo
            5 // 11+ combo
        };

        public static int GetRenAttack(int renCount) => RenAttacks[math.clamp(renCount, 0, 11)];
        public static int GetRenAttack(uint renCount) => RenAttacks[math.min(renCount, 11)];

        public struct PlayerInfo {
            public int ID { get; set; }
            public string Name { get; set; }
            public PlayerKind Kind { get; set; }
        }

        public interface IGameEvent { }

        public class QueueUpdatedEvent : IGameEvent {
            public PieceKind kind;

            public QueueUpdatedEvent(PieceKind kind) {
                this.kind = kind;
            }
        }
        
        public class InitializedEvent : IGameEvent {
            
        }

        public class PieceSpawnedEvent : IGameEvent {
            public PieceKind kind;

            public PieceSpawnedEvent(PieceKind kind) {
                this.kind = kind;
            }
        }

        public class FallingPieceMovedEvent : IGameEvent {
            public static FallingPieceMovedEvent Default { get; } = new FallingPieceMovedEvent();
        }

        public class PieceLockedEvent : IGameEvent {
            public bool hardDrop;
            public Piece piece;
            public LockResult lockResult;

            public PieceLockedEvent(bool hardDrop, Piece piece, LockResult lockResult) {
                this.hardDrop = hardDrop;
                this.piece = piece;
                this.lockResult = lockResult;
            }
        }

        public class HoldEvent : IGameEvent {
            public static HoldEvent Default { get; } = new HoldEvent();
        }

        public class GotDamageEvent : IGameEvent {
            public uint value;

            public GotDamageEvent(uint value) {
                this.value = value;
            }
        }

        public class GarbageLinesAddedEvent : IGameEvent {
            public byte[][] rows;

            public GarbageLinesAddedEvent(byte[][] rows) {
                this.rows = rows;
            }
        }

        public class DeathEvent : IGameEvent {
            public static DeathEvent Default { get; } = new DeathEvent();
        }
    }
}