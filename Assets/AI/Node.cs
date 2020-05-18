using Cyanite.Puzzle;
using Unity.Collections;
using Unity.Mathematics;

namespace Cyanite.AI {
    public struct Node {
        public bool valid;
        public SimpleBoard board;
        
        public int parent;
        public bool holdUsed;
        public ChildrenRef children;
        public Evaluation eval;

        public Node(int parent, Allocator alloc) {
            valid = true;
            var grid = new NativeArray<ushort>(20,alloc);
            board = new SimpleBoard(grid);
            grid.Dispose();
            this.parent = parent;
            children = default;
            eval = default;
            holdUsed = false;
        }

        public Node(int parent, in SimpleBoard prevBoard, in Piece piece, Evaluation evaluation, NativeArray<int4x4> shapeRef) {
            valid = true;
            this.board = prevBoard.AddPieceFast(piece, shapeRef);
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