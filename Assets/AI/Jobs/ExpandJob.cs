using Hikari.Puzzle;
using Unity.Burst;
using Unity.Collections;
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

        public void Execute(int index) {
            if (!selected[index].valid) return;
            if (!selected[index].node.valid) return;
            
            var temp = new NativeList<ExpandResult>(100, Allocator.Temp);

            var spins = selected[index].currentPiece == PieceKind.O ? 1 : 4;

            for (sbyte y = -3; y < 19; y++) {
                for (sbyte x = -3; x < 9; x++) {
                    for (sbyte s = 0; s < spins; s++) {
                        var piece = new Piece(selected[index].currentPiece, x, y, s);
                        
                        if (!boards[selected[index].index].CollidesFast(piece,pieceShapes)
                                    && boards[selected[index].index].GroundedFast(piece,pieceShapes)) {
                            temp.Add(new ExpandResult(selected[index].index,piece, false));
                        }
                    }
                }
            }
            
            expandResultWriter.AddRangeNoResize(temp);
            temp.Dispose();
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