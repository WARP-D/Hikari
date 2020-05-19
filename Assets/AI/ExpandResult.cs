using Cyanite.Puzzle;

namespace Cyanite.AI {
    public struct ExpandResult {
        public Piece placement;
        public int parentIndex;
        public bool holdUsed;

        public ExpandResult(int parentIndex, Piece placement, bool holdUsed) {
            this.parentIndex = parentIndex;
            this.placement = placement;
            this.holdUsed = holdUsed;
        }
    }
}