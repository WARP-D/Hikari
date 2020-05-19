using Hikari.Puzzle;

namespace Hikari.AI {
    public readonly struct ExpandResult {
        public readonly Piece placement;
        public readonly int parentIndex;
        public readonly bool holdUsed;

        public ExpandResult(int parentIndex, Piece placement, bool holdUsed) {
            this.parentIndex = parentIndex;
            this.placement = placement;
            this.holdUsed = holdUsed;
        }
    }
}