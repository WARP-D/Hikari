using Unity.Mathematics;

namespace Hikari.AI {
    public struct Weights {
        public float4 clears;
        public float4 tSpins;
        public float4 tMiniSpins;

        public float tHole;
        public float tstHole;
        public float finHole;
        
        
        public float bumpSum;
        public float bumpMax;

        public static Weights Default => new Weights {
            clears = new float4(-60,-100,-20,300),
            tSpins = new float4(100,400,500,0),
            tMiniSpins = new float4(-100,-500,0,0),
            tHole = 200,
            tstHole = 220,
            finHole = 150,
            bumpSum = -1,
            bumpMax = -1
        };
    }
}