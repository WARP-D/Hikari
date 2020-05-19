using Unity.Mathematics;

namespace Hikari.AI {
    public struct Weights {
        public int4 clears;
        public int4 tSpins;
        public int4 tMiniSpins;

        public int tHole;
        public int tstHole;
        public int finHole;
        
        
        public int bumpSum;
        public int bumpMax;

        public static Weights Default => new Weights {
            clears = new int4(-60,-100,-20,300),
            tSpins = new int4(100,400,500,0),
            tMiniSpins = new int4(-100,-500,0,0),
            tHole = 200,
            tstHole = 220,
            finHole = 150,
            bumpSum = -1,
            bumpMax = -1
        };
    }
}