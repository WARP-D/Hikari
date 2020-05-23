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

            // var spins = selected[index].currentPiece == PieceKind.O ? 1 : 4;
            //
            // for (sbyte y = -3; y < 19; y++) {
            //     for (sbyte x = -3; x < 9; x++) {
            //         for (sbyte s = 0; s < spins; s++) {
            //             var piece = new Piece(selected[index].currentPiece, x, y, s);
            //             
            //             if (!boards[selected[index].index].CollidesFast(piece,pieceShapes)
            //                         && boards[selected[index].index].GroundedFast(piece,pieceShapes)) {
            //                 temp.Add(new ExpandResult(selected[index].index,piece, false));
            //             }
            //         }
            //     }
            // }
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

//         private unsafe void WriteTree(NativeList<Node> nodes) {
//             var idx = Interlocked.Add(ref treeWriter.ListData->Length, nodes.Length) - nodes.Length;
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             var ptr = treeWriter.Ptr;
//             AtomicSafetyHandle.CheckWriteAndThrow(treeSafety[0]);
//             if (idx + nodes.Length > treeWriter.ListData->Capacity)
//                 throw new Exception($"Length {nodes.Length} exceeds capacity Capacity {treeWriter.ListData->Capacity}");
// #endif
//             var size = UnsafeUtility.SizeOf<Node>();
//             void* dst = (byte*)treeWriter.ListData->Ptr + idx * size;
//             UnsafeUtility.MemCpy(dst, treeWriter.ListData->Ptr, nodes.Length * size);
//         }
    }
}