using Unity.Mathematics;

namespace Hikari.AI {
    public struct Node {
        public bool valid;
        
        public bool holdUsed;
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
            visits = 1;
        }

        public Node(int parent, int4 evaluation) {
            valid = true;
            this.parent = parent;
            children = default;
            evalSelf = evaluation;
            evalSum = default;
            holdUsed = false;
            visits = 1;
        }
    }
}