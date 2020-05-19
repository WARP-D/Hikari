using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Cyanite.AI.Jobs {
    [BurstCompile]
    public struct ReorderChildrenJob : IJobParallelForDefer {
        [ReadOnly] public NativeList<ExpandResult> expandResults;
        [ReadOnly] public NativeArray<Evaluation> evaluations;
        [ReadOnly] public NativeArray<int4x4> pieceShapes;
        [WriteOnly] public NativeMultiHashMap<int, NodeWithPiece>.ParallelWriter map;
        
        public void Execute(int index) {
            var er = expandResults[index];
            map.Add(er.parentIndex, new NodeWithPiece(new Node(er.parentIndex, evaluations[index]),er.placement));
        }
    }
}