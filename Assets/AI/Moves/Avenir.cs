using Hikari.Puzzle;
using Unity.Collections;
using Unity.Mathematics;

namespace Hikari.AI {
    public static class Avenir {
        public static NativeHashMap<Piece, Move> Generate(ref SimpleBoard board, Piece spawned, NativeArray<int4x4> pieceShapes) {
            var results = new NativeHashMap<Piece,Move>(200,Allocator.Temp);
            var passed = new NativeHashMap<Piece,bool>(100,Allocator.Temp);
            var checkQueue = new NativeList<Move>(100,Allocator.Temp);
            
            //todo

            passed.Dispose();
            checkQueue.Dispose();
            return results;
        }
    }
}