using Unity.Collections;

namespace Cyanite.AI {
    public struct Node {
        public bool valid;
        
        public int parent;
        public bool holdUsed;
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

        public NativeArray<IndexedNode> GetChildren(NativeArray<Node> tree, Allocator alloc = Allocator.Temp) {
            var array = new NativeArray<IndexedNode>(children.length,alloc);
            var treeSlice = new NativeSlice<Node>(tree,children.start,children.length);
            for (var i = 0; i < children.length; i++) {
                array[i] = new IndexedNode(children.start + i, treeSlice[i]);
            }

            return array;
        }
    }
}