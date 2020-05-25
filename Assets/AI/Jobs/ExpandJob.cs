using Hikari.Puzzle;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct ExpandJob : IJobParallelFor {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<bool> useHold;
        [ReadOnly] public NativeArray<int4x4> pieceShapes;
        [ReadOnly] public NativeArray<SelectResult> selected;
        [ReadOnly] public NativeArray<SimpleBoard> boards;
        public NativeList<ExpandResult>.ParallelWriter expandResultWriter;

        public unsafe void Execute(int index) {
            if (!selected[index].valid) return;
            if (!selected[index].node.valid) return;
            
            var board = boards[index];
            var moves = NextPlacementsGenerator.Generate(ref board, new Piece(selected[index].currentPiece), pieceShapes);
            
            var keys = moves.GetKeyArray(Allocator.Temp);
            var ret = new NativeArray<ExpandResult>(keys.Length, Allocator.Temp,NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < keys.Length; i++) {
                if (moves.TryGetValue(keys[i], out var mv)) {
                    ret[i] = new ExpandResult(selected[index].index, mv.piece, false);
                }
            }
            expandResultWriter.AddRangeNoResize(ret.GetUnsafeReadOnlyPtr(),keys.Length);
            keys.Dispose();
            moves.Dispose();
        }
    }
}