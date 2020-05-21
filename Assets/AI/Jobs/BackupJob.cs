using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct BackupJob : IJob {
        [ReadOnly] public NativeList<ExpandResult> expandResults;
        [ReadOnly] public NativeArray<int4> evaluations;
        public NativeArray<Node> tree;
        
        public void Execute() {
            for (var i = 0; i < expandResults.Length; i++) {
                var parent = expandResults[i].parentIndex;
                var eval = evaluations[i];
                while (true) {
                    var node = tree[parent];
                    node.evalAccumulated += eval;
                    node.visits++;
                    tree[parent] = node;

                    if (node.parent != -1) {
                        parent = node.parent;
                        continue;
                    }

                    break;
                }
            }
        }
    }
}