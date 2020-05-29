using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct ReorderChildrenJob : IJobParallelForDefer {
        [ReadOnly] public NativeList<ExpandResult> expandResults;
        [ReadOnly] public NativeArray<int4> evaluations;
        [WriteOnly] public NativeMultiHashMap<int, Pair<Node,SimpleBoard>>.ParallelWriter map;

        public void Execute(int index) {
            var er = expandResults[index];
            map.Add(er.parentIndex, new Pair<Node, SimpleBoard>(new Node(er.parentIndex, evaluations[index], er.move), er.board));
        }
    }
}