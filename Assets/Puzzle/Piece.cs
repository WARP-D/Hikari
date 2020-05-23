using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Hikari.Puzzle {
    public readonly struct Piece : IEquatable<Piece> {
        public readonly PieceKind kind;
        public readonly sbyte x;
        public readonly sbyte y;
        public readonly sbyte spin;
        public readonly TSpinStatus tSpin;

        public Piece(PieceKind kind, sbyte x, sbyte y, sbyte spin) {
            this.kind = kind;
            this.x = x;
            this.y = y;
            this.spin = spin;
            tSpin = TSpinStatus.None;
        }

        public Piece(PieceKind pieceKind) {
            kind = pieceKind;
            x = 3;
            y = (sbyte) (pieceKind == PieceKind.I ? 17 : 18);
            spin = 0;
            tSpin = TSpinStatus.None;
        }

        private Piece(PieceKind pieceKind, sbyte x, sbyte y, sbyte spin, TSpinStatus spinStatus)
            : this(pieceKind,x,y,spin) {
            tSpin = spinStatus;
        }

        public static readonly ushort[][][] Shapes = { // [type][spin][line]
            new[] { //I
                new ushort[] {0, 0, 15, 0},
                new ushort[] {4, 4, 4, 4},
                new ushort[] {0, 15, 0, 0},
                new ushort[] {2, 2, 2, 2}
            },
            new[] { //O
                new ushort[] {0, 6, 6, 0},
                new ushort[] {0, 6, 6, 0},
                new ushort[] {0, 6, 6, 0},
                new ushort[] {0, 6, 6, 0}
            },
            new[] { //T
                new ushort[] {0, 7, 2, 0},
                new ushort[] {2, 6, 2, 0},
                new ushort[] {2, 7, 0, 0},
                new ushort[] {2, 3, 2, 0}
            },
            new[] { //J
                new ushort[] {0, 7, 1, 0},
                new ushort[] {2, 2, 6, 0},
                new ushort[] {4, 7, 0, 0},
                new ushort[] {3, 2, 2, 0}
            },
            new[] { //L
                new ushort[] {0, 7, 4, 0},
                new ushort[] {6, 2, 2, 0},
                new ushort[] {1, 7, 0, 0},
                new ushort[] {2, 2, 3, 0}
            },
            new[] { //S
                new ushort[] {0, 3, 6, 0},
                new ushort[] {4, 6, 2, 0},
                new ushort[] {3, 6, 0, 0},
                new ushort[] {2, 3, 1, 0}
            },
            new[] { //Z
                new ushort[] {0, 6, 3, 0},
                new ushort[] {2, 6, 4, 0},
                new ushort[] {6, 3, 0, 0},
                new ushort[] {1, 3, 2, 0}
            }
        };

        public static readonly int[] OneDShapes = {
            // I
            0, 0, 15, 0,
            4, 4, 4, 4,
            0, 15, 0, 0,
            2, 2, 2, 2,
            // O
            0, 6, 6, 0,
            0, 6, 6, 0,
            0, 6, 6, 0,
            0, 6, 6, 0,
            // T
            0, 7, 2, 0,
            2, 6, 2, 0,
            2, 7, 0, 0,
            2, 3, 2, 0,
            // J
            0, 7, 1, 0,
            2, 2, 6, 0,
            4, 7, 0, 0,
            3, 2, 2, 0,
            // L
            0, 7, 4, 0,
            6, 2, 2, 0,
            1, 7, 0, 0,
            2, 2, 3, 0,
            // S
            0, 3, 6, 0,
            4, 6, 2, 0,
            3, 6, 0, 0,
            2, 3, 1, 0,
            // Z
            0, 6, 3, 0,
            2, 6, 4, 0,
            6, 3, 0, 0,
            1, 3, 2, 0
        };

        public ushort[] GetShape() {
            return Shapes[(int) kind][spin];
        }

        public void CopyNativeShape(ref NativeArray<int> array) {
            for (var i = 0; i < 4; i++) {
                array[i] = OneDShapes[(int) kind * 16 + spin * 4 + i];
            }
        }

        public IEnumerable<Vector2Int> GetCellPositions() {
            var shape = GetShape();
            for (var i = 0; i < 4; i++) {
                for (var j = 0; j < 4; j++) {
                    if ((shape[i] & (1 << j)) != 0) {
                        yield return new Vector2Int(x + j, y + i);
                    }
                }
            }
        }

        public bool Equals(Piece other) {
            return kind == other.kind && x == other.x && y == other.y && spin == other.spin;
        }

        public override bool Equals(object obj) {
            return obj is Piece other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (int) kind;
                hashCode = (hashCode * 397) ^ x;
                hashCode = (hashCode * 397) ^ y;
                hashCode = (hashCode * 397) ^ spin;
                return hashCode;
            }
        }

        public override string ToString() {
            return $"{kind} ({x},{y}) {spin}";
        }

        public static bool operator ==(Piece left, Piece right) {
            return left.Equals(right);
        }

        public static bool operator !=(Piece left, Piece right) {
            return !left.Equals(right);
        }

        public Piece WithOffset(Vector2Int offset) {
            return new Piece(kind, (sbyte) (x + offset.x), (sbyte) (y + offset.y), spin);
        }

        public Piece WithOffset(int x, int y) {
            return WithOffset(new Vector2Int(x, y));
        }

        public Piece WithOffset(int2 v) {
            return WithOffset(new Vector2Int(v.x, v.y));
        }

        public Piece WithTSpinStatus(TSpinStatus ts) {
            return new Piece(kind, x, y, spin,ts);
        }
    }
}