using Hikari.Puzzle;

namespace Hikari.AI {
    public readonly struct SelectResult {
        public readonly bool valid;
        public readonly IndexedNode iNode;
        public readonly PieceKind currentPiece;

        public Node node => iNode.node;
        public int index => iNode.index;

        public SelectResult(IndexedNode iNode, PieceKind currentPiece) {
            valid = true;
            this.iNode = iNode;
            this.currentPiece = currentPiece;
        }
    }
}