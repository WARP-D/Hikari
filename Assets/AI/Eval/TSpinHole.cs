using Hikari.Puzzle;

namespace Hikari.AI {
    public readonly struct TSpinHole {
        public readonly int lines;
        public readonly Piece piece;

        public TSpinHole(int lines, Piece piece) {
            this.lines = lines;
            this.piece = piece;
        }
    }
}