using System;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Mathematics;

namespace Hikari.AI {
    public static class SRSNoAlloc {
        private static readonly int2x4[] RotationTable = {
            new int2x4(new int2(-1,0),new int2(-1,1),new int2(0,-2),new int2(-1,-2)), //01
            new int2x4(new int2(1,0),new int2(1,-1),new int2(0,2),new int2(1,2)), //10
            new int2x4(new int2(1,0),new int2(1,-1),new int2(0,2),new int2(1,2)), //12
            new int2x4(new int2(-1,0),new int2(-1,1),new int2(0,-2),new int2(-1,-2)), //21
            new int2x4(new int2(1,0),new int2(1,1),new int2(0,-2),new int2(1,-2)), //23
            new int2x4(new int2(-1,0),new int2(-1,-1),new int2(0,2),new int2(-1,2)), //32
            new int2x4(new int2(-1,0),new int2(-1,-1),new int2(0,2),new int2(-1,2)), //30
            new int2x4(new int2(1,0),new int2(1,1),new int2(0,-2),new int2(1,-2)), //03
        };
        
        private static readonly int2x4[] RotationTableI = {
            new int2x4(new int2(-2,0),new int2(1,0),new int2(-2,-1),new int2(1,2)), //01
            new int2x4(new int2(2,0),new int2(-1,0),new int2(2,1),new int2(-1,-2)), //10
            new int2x4(new int2(-1,0),new int2(2,0),new int2(-1,2),new int2(2,-1)), //12
            new int2x4(new int2(1,0),new int2(2,0),new int2(1,-2),new int2(-2,1)), //21
            new int2x4(new int2(2,0),new int2(-1,0),new int2(2,1),new int2(-1,-2)), //23
            new int2x4(new int2(-2,0),new int2(1,0),new int2(-2,-1),new int2(1,2)), //32
            new int2x4(new int2(1,0),new int2(-2,0),new int2(1,-2),new int2(-2,1)), //30
            new int2x4(new int2(-1,0),new int2(2,0),new int2(-1,2),new int2(2,-1)), //03
        };

        public static bool TryRotate(Piece piece, ref SimpleBoard board, bool cw, NativeArray<int4x4> pieceShapes, out int rotation, out Piece rotated) {
            if (piece.kind == PieceKind.O) {
                rotation = 0;
                rotated = piece;
                return true;
            }
            
            var rotatedDirection = GetRotatedDirection(piece.spin, cw);
            
            var newPiece = new Piece(piece.kind, piece.x,piece.y,(sbyte) rotatedDirection);
            if (board.CollidesFast(newPiece, pieceShapes)) {
                rotation = 0;
                rotated = newPiece;
                return true;
            }

            var key = piece.spin * 10 + rotatedDirection;
            var offsetTable = piece.kind == PieceKind.I ? RotationTableI[KeyToIndex(key)] : RotationTable[KeyToIndex(key)];
            
            for (var i = 0; i < 4; i++) {
                newPiece = piece.WithOffset(offsetTable[i]);
                if (board.CollidesFast(newPiece, pieceShapes)) {
                    rotation = i + 1;
                    rotated = newPiece;
                    return true;
                }
            }

            rotation = default;
            rotated = default;
            return false;
        }

        private static int KeyToIndex(int key) {
            switch (key) {
                case 01: return 0;
                case 10: return 1;
                case 12: return 2;
                case 21: return 3;
                case 23: return 4;
                case 32: return 5;
                case 30: return 6;
                case 03: return 7;
                default: throw new ArgumentException();
            }
        }
        
        private static int GetRotatedDirection(int from, bool cw) {
            if (cw) return (from + 1) % 4;
            else return from == 0 ? 3 : from - 1;
        }
    
    }
}