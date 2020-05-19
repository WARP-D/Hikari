using System;
using Hikari.Utils;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Hikari.AI {
    public unsafe struct SimpleBoard {
        public fixed ushort cells[Length];
        public readonly byte backToBack;
        public readonly bool holdingSomething;
        public readonly PieceKind reserve;
        public readonly Bag bag;
        public readonly uint ren;

        private const ushort FilledLine = 0b11111_11111;
        private const int Length = 20;

        private static readonly int[] FullTSpinCheckPoints = {
            0, 0, 2, 0,
            0, 2, 0, 0,
            2, 2, 0, 2,
            2, 0, 2, 2
        };

        private static readonly int[] MiniTSpinCheckPoints = {
            2, 2, 0, 2,
            2, 0, 2, 2,
            0, 0, 2, 0,
            0, 2, 0, 0
        };

        public SimpleBoard(NativeArray<ushort> cells) {
            fixed (ushort* gridPtr = this.cells) {
                UnsafeUtility.MemCpy(gridPtr, cells.GetUnsafeReadOnlyPtr(), Length);
            }

            ren = default; //TODO
            backToBack = default;
            bag = default;
            reserve = default;
            holdingSomething = false;
        }

        public NativeArray<ushort> GetCells() {
            var na = new NativeArray<ushort>(Length, Allocator.Temp);
            fixed (ushort* gridPtr = cells) {
                UnsafeUtility.MemCpy(na.GetUnsafePtr(), gridPtr, Length);
            }

            return na;
        }

        public bool Collides(in Piece piece) {
            var shape = new NativeArray<int>(4,Allocator.Temp);
            piece.CopyNativeShape(ref shape);

            var result = Collides(ref shape, new int2(piece.x, piece.y));

            shape.Dispose();
            return result;
        }

        public bool Collides(ref NativeArray<int> shape, int2 pos) {
            for (var i = 0; i < shape.Length; i++) {
                var y = pos.y + i;

                var pieceLine = shape[i].Shift(pos.x + 3);
                if (y < 0) {
                    if (pieceLine > 0) return true;
                    else continue;
                }

                if (y >= Length) continue;

                var fieldLine = (cells[y] << 3) | 0b111_00000_00000_111;
                if ((pieceLine & fieldLine) != 0) {
                    return true;
                }
            }

            return false;
        }

        public bool CollidesFast(int4 shape, int2 pos) {
            shape <<= pos.x + 3;
            for (var i = 0; i < 4; i++) {
                var y = pos.y + i;
                var pieceLine = shape[i];
                if (y < 0) {
                    if (pieceLine > 0) return true;
                    else continue;
                }

                if (y >= Length) continue;

                var fieldLine = (cells[y] << 3) | 0b111_00000_00000_111;
                if ((pieceLine & fieldLine) != 0) {
                    return true;
                }
            }

            return false;
        }

        public bool CollidesFast(in Piece piece, NativeArray<int4x4> pieceShapes) {
            return CollidesFast(pieceShapes[(int) piece.kind][piece.spin], new int2(piece.x, piece.y));
        }

        public bool Grounded(in Piece piece) {
            if (Collides(piece)) return false;
            var sonicDropped = SonicDrop(piece);
            return sonicDropped == piece;
        }
        public bool GroundedFast(in Piece piece, NativeArray<int4x4> shapeRef) {
            var shape = shapeRef[(int) piece.kind][piece.spin];
            if (CollidesFast(shape, new int2(piece.x,piece.y))) return false;
            var sonicDropped = SonicDropFast(piece,shapeRef);
            return sonicDropped == piece;
        }

        public bool Occupied(int2 at) {
            if (at.x < 0 || 10 <= at.x || at.y < 0) throw new ArgumentOutOfRangeException();
            var line = cells[at.y];
            return (line & (1 << at.x)) > 0;
        }


        private bool Occupied(int x, int y) => Occupied(new int2(x, y));

        public Piece SonicDrop(in Piece piece) {
            var shape = new NativeArray<int>(4,Allocator.Temp);
            piece.CopyNativeShape(ref shape);
            
            if (Collides(ref shape, new int2(piece.x,piece.y))) return piece;
            
            var prevDrop = piece;
            for (var i = (sbyte) (piece.y - 1); i >= -3; i--) {
                var drop = new Piece(piece.kind, piece.x, i, piece.spin);
                if (Collides(ref shape, new int2(piece.x, i))) {
                    shape.Dispose();
                    return prevDrop;
                }

                prevDrop = drop;
            }

            shape.Dispose();
            return prevDrop;
        }
        
        public Piece SonicDropFast(in Piece piece, NativeArray<int4x4> shapeRef) {
            var shape = shapeRef[(int) piece.kind][piece.spin];
            if (CollidesFast(shape, new int2(piece.x,piece.y))) return piece;
            
            var prevDrop = piece;
            for (var i = (sbyte) (piece.y - 1); i >= -3; i--) {
                var drop = new Piece(piece.kind, piece.x, i, piece.spin);
                if (CollidesFast(shape, new int2(piece.x, i))) {
                    return prevDrop;
                }

                prevDrop = drop;
            }
            
            return prevDrop;
        }

        public SimpleBoard AddPiece(in Piece piece) {
            var shape = new NativeArray<int>(4,Allocator.Temp);
            piece.CopyNativeShape(ref shape);
            var grid = new NativeArray<ushort>(Length, Allocator.Temp);
            fixed (ushort* cellsPtr = cells) {
                UnsafeUtility.MemCpy(grid.GetUnsafePtr(), cellsPtr, Length);
            }

            for (var i = 0; i < 4; i++) {
                var y = i + piece.y;

                if (y >= Length) break;
                if (y < 0) continue;

                cells[y] |= (ushort) (shape[i] << piece.x);
            }

            shape.Dispose();

            var ret = new SimpleBoard(grid);
            grid.Dispose();
            return ret;
        }
        
        public SimpleBoard AddPieceFast(in Piece piece, NativeArray<int4x4> shapeRef) {
            var x = (int)piece.x;
            var shape = shapeRef[(int) piece.kind][piece.spin] << x;
            
            var newBoard = new SimpleBoard();
            
            for (var i = 0; i < 4; i++) {
                var y = i + piece.y;
            
                if (y >= Length) break;
                if (y < 0) continue;
            
                newBoard.cells[y] = (ushort) (cells[y] | shape[i]);
            }
            
            return newBoard;
        }

        public TSpinStatus CheckTSpin(Piece piece, int rotation) {
            if (piece.kind != PieceKind.T) return TSpinStatus.None;
            var checkIndex = piece.spin * 4;
            var tSpinCheckCount = 0;
            if (Occupied(FullTSpinCheckPoints[checkIndex] + piece.x, FullTSpinCheckPoints[checkIndex + 1] + piece.y)) {
                tSpinCheckCount++;
            }

            if (Occupied(FullTSpinCheckPoints[checkIndex + 2] + piece.x,
                FullTSpinCheckPoints[checkIndex + 3] + piece.y)) {
                tSpinCheckCount++;
            }

            var miniTSpinCheckCount = 0;
            if (Occupied(MiniTSpinCheckPoints[checkIndex] + piece.x, MiniTSpinCheckPoints[checkIndex + 1] + piece.y)) {
                miniTSpinCheckCount++;
            }

            if (Occupied(MiniTSpinCheckPoints[checkIndex + 2] + piece.x,
                MiniTSpinCheckPoints[checkIndex + 3] + piece.y)) {
                miniTSpinCheckCount++;
            }


            if (tSpinCheckCount + miniTSpinCheckCount >= 3) {
                if (rotation == 4) return TSpinStatus.Full;
                else if (miniTSpinCheckCount == 2) return TSpinStatus.Full;
                else return TSpinStatus.Mini;
            } else return TSpinStatus.None;
        }

        public SimpleLockResult Lock(in Piece piece, Allocator alloc, out SimpleBoard output) {
            var board = AddPiece(piece);
        
            var clearedLines = new NativeList<int>(4, Allocator.Temp);
        
            for (var i = 0; i < Length; i++) {
                if (cells[i] == FilledLine) clearedLines.Add(i);
            }
        
            for (var i = 0; i < clearedLines.Length; i++) {
                for (var j = clearedLines[i]; j < Length - 1; j++) {
                    cells[j] = cells[j + 1];
                }
        
                cells[Length - 1 - i] = 0;
            }
        
            output = board;
        
            return new SimpleLockResult();
        }
    }
}