using System.Runtime.InteropServices;

namespace Cyanite.Addons.ColdClear {
    [StructLayout(LayoutKind.Sequential)]
    public struct CCMove {
        /// <summary>
        /// Whether hold is required
        /// </summary>
        [MarshalAs(UnmanagedType.U1)] public bool hold;

        /// <summary>
        /// Expected cell coordinates of placement, (0, 0) being the bottom left
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] expected_x;

        /// <summary>
        /// Expected cell coordinates of placement, (0, 0) being the bottom left
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] expected_y;

        /// <summary>
        /// Number of moves in the path
        /// </summary>
        public byte movement_count;

        /// <summary>
        /// Movements
        /// Length is always 32 so use <c>movement_count</c> as true length
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public CCMovement[] movements;

        public uint nodes;
        public uint depth;
        public uint original_rank;
    }
}