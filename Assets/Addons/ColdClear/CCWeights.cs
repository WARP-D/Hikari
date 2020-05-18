using System.Runtime.InteropServices;

namespace Cyanite.Addons.ColdClear {
    [StructLayout(LayoutKind.Sequential)]
    public struct CCWeights {
        public int back_to_back;
        public int bumpiness;
        public int bumpiness_sq;
        public int height;
        public int top_half;
        public int top_quarter;
        public int jeopardy;
        public int cavity_cells;
        public int cavity_cells_sq;
        public int overhang_cells;
        public int overhang_cells_sq;
        public int covered_cells;
        public int covered_cells_sq;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] tslot;

        public int well_depth;
        public int max_well_depth;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public int[] well_column;

        public int b2b_clear;
        public int clear1;
        public int clear2;
        public int clear3;
        public int clear4;
        public int tspin1;
        public int tspin2;
        public int tspin3;
        public int mini_tspin1;
        public int mini_tspin2;
        public int perfect_clear;
        public int combo_garbage;
        public int move_time;
        public int wasted_t;
        [MarshalAs(UnmanagedType.U1)] public bool use_bag;
    }
}