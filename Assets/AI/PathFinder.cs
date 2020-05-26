using Hikari.Puzzle;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI {
    public static class PathFinder {
        public static Move? FindPath(ref SimpleBoard board, Piece placement, NativeArray<int4x4> pieceShapes) {
            var job = new PathFindJob {
                board = board,
                dest = placement,
                pieceShapes = pieceShapes,
                success = new NativeArray<bool>(1, Allocator.TempJob),
                move = new NativeArray<Move>(1, Allocator.TempJob)
            };
            job.Run();
            var success = job.success[0];
            var mv = job.move[0];
            job.success.Dispose();
            job.move.Dispose();
            return success ? mv : (Move?) null;
        }

        [BurstCompile]
        private struct PathFindJob : IJob {
            public SimpleBoard board;
            public Piece dest;
            [ReadOnly] public NativeArray<int4x4> pieceShapes;

            [WriteOnly] public NativeArray<Move> move;
            [WriteOnly] public NativeArray<bool> success;

            public void Execute() {
                var paths = NextPlacementsGenerator.Generate(ref board, new Piece(dest.kind), pieceShapes);
                if (paths.TryGetValue(dest, out var m)) {
                    move[0] = m;
                    success[0] = true;
                }

                paths.Dispose();
            }
        }
    }
}