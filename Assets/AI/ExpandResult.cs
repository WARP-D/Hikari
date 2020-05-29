namespace Hikari.AI {
    public readonly struct ExpandResult {
        public readonly Move move;
        public readonly int parentIndex;
        public readonly SimpleBoard board;
        public readonly SimpleLockResult placement;

        public ExpandResult(int parentIndex, Move move, SimpleBoard board, SimpleLockResult placement) {
            this.parentIndex = parentIndex;
            this.move = move;
            this.board = board;
            this.placement = placement;
        }
    }
}