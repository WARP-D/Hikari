using Hikari.Puzzle;
using Unity.Mathematics;

namespace Hikari.AI {
    public struct Node {
        public bool valid;

        public Piece piece;
        public bool holdUsed;
        public bool holdOnly;
        public int parent;
        public ChildrenRef children;
        public int4 evalSelf;
        public int4 evalSum;
        public int visits;

        public Node(int parent) {
            valid = true;
            this.parent = parent;
            children = default;
            evalSelf = default;
            evalSum = default;
            holdUsed = false;
            holdOnly = false;
            visits = 1;
            piece = default;
        }

        public Node(int parent, int4 evaluation, Move mv) {
            valid = true;
            this.parent = parent;
            children = default;
            evalSelf = evaluation;
            evalSum = default;
            holdUsed = mv.hold;
            holdOnly = mv.holdOnly;
            visits = 1;
            piece = mv.piece;
        }
    }
}