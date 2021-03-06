using Hikari.Puzzle;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public unsafe struct EvaluateJob : IJobParallelForDefer {
        [ReadOnly] public NativeArray<ExpandResult> inputs;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Weights> weight;
        [WriteOnly] public NativeArray<int4> results;

        public void Execute(int index) {
            var ex = inputs[index];

            if (ex.move.holdOnly) {
                results[index] = new int4(0,0,0,0);
                return;
            }

            var w = weight[0];

            var fieldSafety = 10000; //Represents how we can stand enemy's attack and dig garbage
            var fieldPower = 0; //Represents how we can send damage
            var moveScore = 0;

            var lr = ex.placement;
            var board = ex.board;
            var columns = stackalloc int[10];
            var maxHeights = stackalloc byte[10];
            board.GetColumns(columns, maxHeights);

            var holeColumn = CalcHolePos(ref maxHeights);
            var bumpiness = CalcBumpiness(ref maxHeights, holeColumn);

            // ReSharper disable once UselessBinaryOperation
            fieldSafety += bumpiness.x * w.bumpSum;
            fieldSafety += bumpiness.y * bumpiness.y * w.bumpMax;

            var maxHeight = CalcMaxHeight(ref board);
            fieldSafety += w.maxHeight * maxHeight;

            fieldSafety += UnreachableHoles(ref board, maxHeight) * w.closedHoles;

            if (lr.perfectClear) moveScore += w.perfect;
            moveScore += w.ren * Game.GetRenAttack(lr.ren);
            moveScore += lr.placementKind switch {
                PlacementKind.Clear1 => w.clear1,
                PlacementKind.Clear2 => w.clear2,
                PlacementKind.Clear3 => w.clear3,
                PlacementKind.Clear4 => w.clear4,
                PlacementKind.Mini1 => w.tMini1,
                PlacementKind.Mini2 => w.tMini2,
                PlacementKind.TSpin1 => w.tSpin1,
                PlacementKind.TSpin2 => w.tSpin2,
                PlacementKind.TSpin3 => w.tSpin3,
                _ => 0
            };

            if (ex.move.piece.kind == PieceKind.T) {
                switch (lr.placementKind) {
                    case PlacementKind.TSpin1:
                    case PlacementKind.TSpin2:
                    case PlacementKind.TSpin3:
                        break;
                    default:
                        moveScore += w.wastedT;
                        break;
                }
            }


            results[index] = new int4(fieldSafety, fieldPower, moveScore, 0);
        }

        private static int CalcHolePos(ref byte* cMaxHeights) {
            var minX = 0;
            var minVal = SimpleBoard.Length;
            for (var i = 0; i < 10; i++) {
                if (minVal > cMaxHeights[i]) {
                    minX = i;
                    minVal = cMaxHeights[i];
                }
            }

            return minX;
        }

        private static int2 CalcBumpiness(ref byte* cMaxHeights, int holeColumn) {
            var sum = 0;
            var max = 0;
            for (var i = 0; i < 9; i++) {
                if (i == holeColumn || i + 1 == holeColumn) continue;
                var abs = math.abs(cMaxHeights[i] - cMaxHeights[i + 1]);
                sum += abs;
                max = math.max(max, abs);
            }

            return new int2(sum, max);
        }

        private static int CalcMaxHeight(ref SimpleBoard board) {
            var max = 0;
            for (var i = 0; i < SimpleBoard.Length; i++) {
                if (board.cells[i] != 0) max = i;
            }

            return max;
        }

        private static int UnreachableHoles(ref SimpleBoard board, int maxHeight) {
            var count = 0;
            for (var y = 0; y < maxHeight - 2; y++) {
                count += math.countbits(~board.cells[y] & (board.cells[y + 1] | board.cells[y + 2]));
            }

            return count;
        }
    }
}