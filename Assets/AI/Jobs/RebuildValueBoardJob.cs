using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct RebuildValueBoardJob : IJobParallelFor {
        [ReadOnly] public NativeArray<SimpleBoard> input;
        [WriteOnly] public NativeArray<ValueBoard> output;

        public void Execute(int index) {
            var na = new NativeArray<ushort>(40, Allocator.Persistent);
            var nativeArray = input[index].GetCells();
            NativeArray<ushort>.Copy(nativeArray, na);
            nativeArray.Dispose();
            output[index] = new ValueBoard {
                b2b = input[index].backToBack,
                bag = input[index].bag,
                cells = na
                //todo
            };
        }
    }
}