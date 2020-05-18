using System;
using System.Runtime.InteropServices;

namespace Cyanite.Addons.ColdClear {
    internal static class ColdClearInterface {
        [DllImport("libcold_clear")]
        public static extern IntPtr cc_launch_async(CCOptions options, CCWeights weights);

        [DllImport("libcold_clear")]
        public static extern void cc_destroy_async(IntPtr bot);

        [DllImport("libcold_clear")]
        public static extern void cc_reset_async(IntPtr bot,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)]
            bool[] field,
            [MarshalAs(UnmanagedType.U1)] bool b2b,
            uint combo);

        [DllImport("libcold_clear")]
        public static extern void cc_add_next_piece_async(IntPtr bot, CCPiece piece);

        [DllImport("libcold_clear")]
        public static extern void cc_request_next_move(IntPtr bot, uint incoming);

        [DllImport("libcold_clear")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool cc_poll_next_move(IntPtr bot, out CCMove move);

        [DllImport("libcold_clear")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool cc_is_dead_async(IntPtr bot);

        [DllImport("libcold_clear")]
        public static extern void cc_default_options(out CCOptions options);

        [DllImport("libcold_clear")]
        public static extern void cc_default_weights(out CCWeights weights);

        [DllImport("libcold_clear")]
        public static extern void cc_fast_weights(out CCWeights weights);
    }
}