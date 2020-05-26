using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct CreateRngsJob : IJob {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Random> rng;
        [WriteOnly] public NativeArray<Random> outputs;

        public void Execute() {
            var random = rng[0];
            for (var i = 0; i < outputs.Length; i++) {
                outputs[i] = new Random(random.NextUInt(1, uint.MaxValue));
            }
        }
    }
}