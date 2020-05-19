namespace Hikari.AI {
    public struct Node {
        public bool valid;
        
        public bool holdUsed;
        public int parent;
        public ChildrenRef children;
        public Evaluation eval;

        public Node(int parent) {
            valid = true;
            this.parent = parent;
            children = default;
            eval = default;
            holdUsed = false;
        }

        public Node(int parent, Evaluation evaluation) {
            valid = true;
            this.parent = parent;
            children = default;
            eval = evaluation;
            holdUsed = false;
        }
    }
}