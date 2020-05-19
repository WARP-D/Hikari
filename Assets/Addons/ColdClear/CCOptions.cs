using System.Runtime.InteropServices;

namespace Hikari.Addons.ColdClear {
    [StructLayout(LayoutKind.Sequential)]
    public struct CCOptions {
        public CCMovementMode mode;
        [MarshalAs(UnmanagedType.U1)] public bool use_hold;
        [MarshalAs(UnmanagedType.U1)] public bool speculate;
        public uint min_nodes;
        public uint max_nodes;
        public uint threads;
    }
}