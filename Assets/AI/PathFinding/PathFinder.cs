using Cyanite.Puzzle;
using Unity.Collections;
using Unity.Mathematics;

namespace Cyanite.AI {
    public static class PathFinder {
        // public static NativeArray<Command> FindPath(ref SimpleBoard board, Piece piece) {
        //     
        // }

        private struct RewindStep {
            public sbyte x;
            public sbyte y;
            public sbyte spin;
            public RewindType type;
        }
        
        private enum RewindType : byte {
            Drop,
            Left,
            Right,
            Cw,
            Ccw
        }
    }
}