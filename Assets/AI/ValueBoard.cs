using Hikari.Puzzle;
using Unity.Collections;

namespace Hikari.AI {
    public struct ValueBoard {
        public NativeArray<ushort> cells;
        public uint ren;
        public byte b2b;
        public PieceKind? hold;
        public NativeQueue<PieceKind> queue;
        public Bag bag;
    }
}