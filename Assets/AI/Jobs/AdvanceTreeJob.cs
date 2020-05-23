using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct AdvanceTreeJob : IJob {
        [ReadOnly] public NativeList<Node> tree;
        [ReadOnly] public NativeList<SimpleBoard> boards;
        public NativeList<Node> advancedTree;
        [WriteOnly] public NativeList<SimpleBoard> advancedBoards;
        public int root;
        public int picked;
        
        public void Execute() {
            if (picked >= tree[root].children.start + tree[root].children.length || tree[root].children.start > picked)
                throw new ArgumentOutOfRangeException();

            var rootNode = tree[picked];
            rootNode.parent = -1;
            advancedTree.Add(rootNode);
            advancedBoards.Add(boards[picked]);
            AddChildren(rootNode.children,0);
        }

        private void AddChildren(ChildrenRef children, int movedParentIndex) {
            var idx = advancedTree.Length;
            for (var i = 0; i < children.length; i++) {
                var child = tree[children.start + i];
                child.parent = movedParentIndex;
                child.children = default;
                advancedTree.Add(child);
                advancedBoards.Add(boards[children.start + i]);
            }

            var parent = advancedTree[movedParentIndex];
            parent.children = new ChildrenRef(children.length == 0 ? 0 : idx, children.length);
            advancedTree[movedParentIndex] = parent;

            for (var i = 0; i < children.length; i++) {
                AddChildren(tree[children.start + i].children, idx + i);
            }
        }
    }
}