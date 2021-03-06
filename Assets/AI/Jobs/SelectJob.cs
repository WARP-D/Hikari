using Hikari.Puzzle;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct SelectJob : IJobParallelFor {
        [ReadOnly] public NativeArray<Node> tree;
        [ReadOnly] public NativeArray<PieceKind> pieceQueue;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<int> rootIndex;
        [ReadOnly] public NativeArray<Random> rands;
        [WriteOnly] public NativeArray<SelectResult> selected;
        [WriteOnly] public NativeArray<int> depths;
        [WriteOnly] public NativeArray<int> retryCounts;

        public void Execute(int i) {
            var retryCount = 0;
            var rng = rands[i];

            exec:
            var current = new IndexedNode(rootIndex[0], tree[rootIndex[0]]);

            var depth = 0;
            while (current.node.children.length > 0) {
                depth++;
                if (depth >= pieceQueue.Length) {
                    if (retryCount++ > 10) {
                        retryCounts[i] = retryCount;
                        return;
                    }

                    goto exec;
                }

                Select(ref current, ref rng);
            }

            var selectResult = new SelectResult(current, pieceQueue[depth]);
            selected[i] = selectResult;
            depths[i] = depth;
            retryCounts[i] = retryCount;
        }

        private unsafe void Select(ref IndexedNode current, ref Random rng) {
            var children = current.node.children;
            var weights = stackalloc float[children.length];
            // var weights = new NativeArray<float>(children.length,Allocator.Temp);
            var sum = 0f;
            var min = 0f;
            for (var j = 0; j < children.length; j++) {
                var child = tree[children.start + j];

                var q = 100f * (child.visits != 0 ? 10 * SumInt4(child.evalSum) / child.visits : 0);
                var u = 0.01f * math.sqrt(current.node.visits) / (1 + child.visits);
                var s = 1 * 4 * SumInt4(current.node.evalSelf);

                var a = q + u + s;
                weights[j] = a;
                sum += a;
                if (a < min) min = a;
            }

            sum += (100 - min) * children.length;

            for (var j = 0; j < children.length; j++) {
                weights[j] = (weights[j] - min + 100) / sum;
            }

            var rand = rng.NextFloat(0f, 1f);
            var val = 0f;

            for (var j = 0; j < children.length; j++) {
                val += weights[j];
                if (val > rand) {
                    var idx = children.start + j;
                    current = new IndexedNode(idx, tree[idx]);
                    break;
                }
            }

            // weights.Dispose();
        }

        private static int SumInt4(int4 i4) {
            return i4.x + i4.y + i4.z + i4.w;
        }
    }
}