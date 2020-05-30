using System;
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
            var sel = selected[index];
            if (!sel.valid) return;
            if (!sel.node.valid) return;

            var board = boards[sel.index];

            var ret = new NativeList<ExpandResult>(200, Allocator.Temp);
            Expand(index, ref board, sel.currentPiece, ref ret, false);
            if (useHold[0] && !sel.node.holdOnly) {
                if (board.hold.HasValue && board.hold.Value != sel.currentPiece) {
                    Expand(index, ref board, sel.currentPiece, ref ret, true);
                } else {
                    // The first hold also makes queue advance 1 step, so we make this "hold only" node
                    var hOnlyMove = new Move {
                        holdOnly = true
                    };
                    ret.Add(new ExpandResult(sel.index, hOnlyMove,
                        board.WithHold(sel.currentPiece, true), default));
                }
            }

            expandResultWriter.AddRangeNoResize(ret.GetUnsafeReadOnlyPtr(), ret.Length);
            ret.Dispose();
        }

        private void Expand(int index, ref SimpleBoard board, PieceKind spawned, ref NativeList<ExpandResult> ret,
            bool useHold) {
            var usePiece = useHold ? board.hold!.Value : spawned;
            var spawn = board.Spawn(usePiece);
            if (!spawn.HasValue) return;
            var moves = NextPlacementsGenerator.Generate(ref board, spawn.Value, pieceShapes, useHold);

            var keys = moves.GetKeyArray(Allocator.Temp);
            for (var i = 0; i < keys.Length; i++) {
                if (moves.TryGetValue(keys[i], out var mv)) {
                    var lr = board.LockFast(mv.piece, useHold ? spawned : (PieceKind?) null, pieceShapes, 
                        out var b1, true);
                    ret.Add(new ExpandResult(selected[index].index, mv, b1, lr));
                } else throw new Exception();
            }

            keys.Dispose();
            moves.Dispose();
        }
    }
}