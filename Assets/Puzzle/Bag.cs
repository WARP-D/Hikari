using System;

namespace Hikari.Puzzle {
    public struct Bag {
        private byte value;
        private byte count;

        private byte TakeRandom() {
            if (value == 0) RestoreAll();
            var n = (byte)(UnityEngine.Random.Range(0,count) & 0xf);
            byte a = 0;
            for (byte i = 0; i < 7; i++) {
                if ((value & (1 << i)) <= 0) continue;
                if (a++ != n) continue;
                
                value -= (byte)(1 << i);
                count--;
                return i;
            }

            throw new Exception();
        }

        public PieceKind TakeRandomPiece() => Pieces[TakeRandom()];

        // ReSharper disable once MemberCanBePrivate.Global
        public void RestoreAll() {
            value = 0b1111111;
            count = 7;
        }

        private static readonly PieceKind[] Pieces;

        static Bag() {
            Pieces = (PieceKind[]) Enum.GetValues(typeof(PieceKind));
        }
    }
}