using Hikari.Puzzle;

namespace Hikari.AI {
    public readonly struct NodeWithPiece {
        public readonly Node node;
        public readonly Piece piece;

        public NodeWithPiece(Node node, Piece piece) {
            this.node = node;
            this.piece = piece;
        }
    }
}