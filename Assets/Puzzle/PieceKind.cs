using System;
using static Hikari.Puzzle.PieceKind;

namespace Hikari.Puzzle {
    public enum PieceKind : byte {
        I,
        O,
        T,
        J,
        L,
        S,
        Z
    }

    public static class PieceKindExtensions {
        public static byte GetIndex(this PieceKind kind) {
            switch (kind) {
                case I: return 0;
                case O: return 1;
                case T: return 2;
                case J: return 3;
                case L: return 4;
                case S: return 5;
                case Z: return 6;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}