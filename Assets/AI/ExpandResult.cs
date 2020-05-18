using Cyanite.Puzzle;

namespace Cyanite.AI {
    public struct ExpandResult {
        public SimpleBoard board;
        public Piece placement;
        public int parentIndex;
        public bool holdUsed;

        public ExpandResult(SimpleBoard board, int parentIndex, Piece placement, bool holdUsed) {
            this.board = board;
            this.parentIndex = parentIndex;
            this.placement = placement;
            this.holdUsed = holdUsed;
        }
    }
}