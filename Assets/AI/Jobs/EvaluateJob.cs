using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public unsafe struct EvaluateJob : IJobParallelForDefer {
        [ReadOnly] public NativeArray<SimpleBoard> boards;
        [ReadOnly] public NativeArray<ExpandResult> inputs;
        [ReadOnly,DeallocateOnJobCompletion] public NativeArray<Weights> weight;
        [ReadOnly] public NativeArray<int4x4> pieceShapes;
        [WriteOnly] public NativeArray<int4> results;

        public void Execute(int index) {
            var ex = inputs[index];
            var pl = ex.placement;
            var w = weight[0];

            var defense = 0;
            var offense = 0;

            var board = boards[ex.parentIndex].AddPieceFast(pl,pieceShapes);
            var columns = stackalloc int[10];
            var maxHeights = stackalloc byte[10];
            board.GetColumns(columns, maxHeights);

            var holeColumn = CalcHolePos(ref maxHeights);
            var bumpiness = CalcBumpiness(ref maxHeights, holeColumn);
            
            defense += (int)(bumpiness.x * w.bumpSum);
            defense += (int) (math.pow(bumpiness.y, 1.3f) * w.bumpMax);

            defense += 200 - 10 * CalcMaxHeight(ref board);

            results[index] = new int4(defense,offense,0,0);
        }

        private static int CalcHolePos(ref byte* cMaxHeights) {
            var minX = 0;
            var minVal = 20;
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
                if (i == holeColumn || i+1 == holeColumn) continue;
                var abs = math.abs(cMaxHeights[i] - cMaxHeights[i + 1]);
                sum += abs;
                max = math.max(max, abs);
            }

            return new int2(sum,max);
        }

        private static int CalcMaxHeight(ref SimpleBoard board) {
            var max = 0;
            for (var i = 0; i < 20; i++) {
                if (board.cells[i] != 0) max = i;
            }

            return max;
        }

        // private static int2 FindTSpinHole(ref NativeArray<ushort> grid) {
        //     var shape = new int3(0b101,0b000,0b001);
        //     for (var y = 0; y < grid.Length-2; y++) {
        //         for (var x = 0; x < 7; x++) {
        //             var s = shape << x;
        //             if ((grid[y] & shape.x) == 0 ||
        //                 (grid[y + 1] & shape.y) == 0 ||
        //                 (grid[y + 2] & shape.z) == 0) {
        //                 
        //             }
        //         }
        //     }
        // }
    }
}