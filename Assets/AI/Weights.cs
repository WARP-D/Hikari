namespace Hikari.AI {
    public struct Weights {
        public int clear1;
        public int clear2;
        public int clear3;
        public int clear4;
        public int tSpin1;
        public int tSpin2;
        public int tSpin3;
        public int tMini1;
        public int tMini2;

        public int perfect;

        public int tHole;
        public int tstHole;
        public int finHole;

        public int b2bContinue;
        public int ren;
        
        public int wastedT;

        public int bumpSum;
        public int bumpMax;

        public int maxHeight;
        public int closedHoles;

        public static Weights Default => new Weights {
            clear1 = 10000,
            clear2 = 10000,
            clear3 = 10000,
            clear4 = 10000,
            tSpin1 = 50,
            tSpin2 = 450,
            tSpin3 = 600,
            tMini1 = -100,
            tMini2 = -500,
            perfect = 1000,
            tHole = 200,
            tstHole = 220,
            finHole = 150,
            b2bContinue = 100,
            ren = 15,
            wastedT = -25,
            bumpSum = -100,
            bumpMax = -1000,
            maxHeight = -50,
            closedHoles = -400
        };
    }
}