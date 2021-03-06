using System;
using System.Linq;
using Hikari.Utils;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Hikari.AI {
    public unsafe struct SimpleBoard {
        public fixed ushort cells[Length];
        public bool backToBack;
        public PieceKind? hold;
        public Bag bag;
        public uint ren;

        private const ushort FilledLine = 0b11111_11111;
        public const int Length = 20;

        private static readonly int4[] FullTSpinCheckPoints = {
            new int4(0, 0, 2, 0),
            new int4(0, 2, 0, 0),
            new int4(2, 2, 0, 2),
            new int4(2, 0, 2, 2)
        };

        private static readonly int4[] MiniTSpinCheckPoints = {
            new int4(2, 2, 0, 2),
            new int4(2, 0, 2, 2),
            new int4(0, 0, 2, 0),
            new int4(0, 2, 0, 0)
        };

        public SimpleBoard(NativeArray<ushort> cells) {
            fixed (ushort* gridPtr = this.cells) {
                UnsafeUtility.MemCpy(gridPtr, cells.GetUnsafeReadOnlyPtr(), Length);
            }

            ren = default; //TODO
            backToBack = default;
            bag = default;
            hold = null;
        }

        public SimpleBoard(Board board) {
            for (var i = 0; i < Length; i++) {
                cells[i] = board.row[i].ToBitFlags();
            }

            ren = board.ren;
            backToBack = board.b2b;
            bag = board.bag;
            hold = board.holdPiece;
        }

        public NativeArray<ushort> GetCells() {
            var na = new NativeArray<ushort>(Length, Allocator.Temp);
            fixed (ushort* gridPtr = cells) {
                UnsafeUtility.MemCpy(na.GetUnsafePtr(), gridPtr, Length);
            }

            return na;
        }

        public Piece? Spawn(PieceKind pk) {
            var piece = new Piece(pk,3,(sbyte) (pk == PieceKind.I ? 17 : 18), 0);
            if (Collides(piece)) piece = piece.WithOffset(0, 1);
            return Collides(piece) ? (Piece?) null : piece;
        }

        public bool Collides(in Piece piece) {
            var shape = new NativeArray<int>(4, Allocator.Temp);
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

                if (y >= Length) return false;

                var fieldLine = (cells[y] << 3) | 0b111_00000_00000_111;

                if ((pieceLine & fieldLine) != 0) return true;
            }

            return false;
        }

        public bool CollidesFast(in Piece piece, NativeArray<int4x4> pieceShapes) {
            return CollidesFast(pieceShapes[(int) piece.kind][piece.spin], new int2(piece.x, piece.y));
        }

        [Obsolete]
        public bool Grounded(in Piece piece) {
            if (Collides(piece)) return false;
            var sonicDropped = SonicDrop(piece);
            return sonicDropped == piece;
        }

        public bool GroundedFast(in Piece piece, NativeArray<int4x4> shapeRef) {
            var shape = shapeRef[(int) piece.kind][piece.spin];
            if (CollidesFast(shape, new int2(piece.x, piece.y))) return false;
            var sonicDropped = SonicDropFast(piece, shapeRef);
            return sonicDropped == piece;
        }

        public bool Occupied(int2 at) {
            if (at.x < 0 || 10 <= at.x || at.y < 0) return true;
            var line = cells[at.y];
            return (line & (1 << at.x)) > 0;
        }

        public bool Occupied(int x, int y) => Occupied(new int2(x, y));

        public Piece SonicDrop(in Piece piece) {
            var shape = new NativeArray<int>(4, Allocator.Temp);
            piece.CopyNativeShape(ref shape);

            if (Collides(ref shape, new int2(piece.x, piece.y))) return piece;

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
            if (CollidesFast(shape, new int2(piece.x, piece.y))) return piece;

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

        [Obsolete]
        public SimpleBoard AddPiece(in Piece piece) {
            var shape = new NativeArray<int>(4, Allocator.Temp);
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
            var x = (int) piece.x;
            var shape = x >= 0
                ? shapeRef[(int) piece.kind][piece.spin] << x
                : shapeRef[(int) piece.kind][piece.spin] >> -x;

            var newBoard = new SimpleBoard {bag = bag, hold = hold};

            fixed (ushort* cPtr = cells) UnsafeUtility.MemCpy(newBoard.cells, cPtr, sizeof(ushort) * Length);

            for (var i = 0; i < 4; i++) {
                var y = i + piece.y;

                if (y >= Length) break;
                if (y < 0) continue;

                newBoard.cells[y] = (ushort) (newBoard.cells[y] | shape[i]);
            }

            return newBoard;
        }

        public void GetColumns(int* columns, byte* cMaxHeights) {
            for (byte y = 0; y < 20; y++) {
                for (byte x = 0; x < 10; x++) {
                    if ((cells[y] & (1 << x)) > 0) {
                        cMaxHeights[x] = y;
                        columns[x] |= 1 << y;
                    }
                }
            }
        }

        public TSpinStatus CheckTSpin(Piece piece, int rotation) {
            if (piece.kind != PieceKind.T) return TSpinStatus.None;
            var tSpinCheckCount = 0;
            var pos = new int2(piece.x, piece.y);
            if (Occupied(FullTSpinCheckPoints[piece.spin].xy + pos)) {
                tSpinCheckCount++;
            }

            if (Occupied(FullTSpinCheckPoints[piece.spin].zw + pos)) {
                tSpinCheckCount++;
            }

            var miniTSpinCheckCount = 0;
            if (Occupied(MiniTSpinCheckPoints[piece.spin].xy + pos)) {
                miniTSpinCheckCount++;
            }

            if (Occupied(MiniTSpinCheckPoints[piece.spin].zw + pos)) {
                miniTSpinCheckCount++;
            }


            if (tSpinCheckCount + miniTSpinCheckCount >= 3) {
                if (rotation == 4) return TSpinStatus.Full;
                else if (miniTSpinCheckCount == 2) return TSpinStatus.Full;
                else return TSpinStatus.Mini;
            } else return TSpinStatus.None;
        }

        public SimpleLockResult LockFast(in Piece piece, PieceKind? heldPiece, NativeArray<int4x4> pieceShapes, out SimpleBoard nextBoard, bool useBag) {
            nextBoard = AddPieceFast(piece,pieceShapes);

            if (heldPiece.HasValue) {
                nextBoard.hold = heldPiece.Value;
            }

            var clearedLines = new NativeList<int>(4, Allocator.Temp);

            for (var i = 0; i < Length; i++) {
                if (nextBoard.cells[i] == FilledLine) clearedLines.Add(i);
            }

            for (var i = 0; i < clearedLines.Length; i++) {
                for (var j = clearedLines[i]; j < Length - 1; j++) {
                    nextBoard.cells[j] = nextBoard.cells[j + 1];
                }

                nextBoard.cells[Length - 1 - i] = 0;
            }

            var pc = true;
            for (var i = 0; i < Length; i++) {
                if (nextBoard.cells[i] != 0) pc = false;
            }

            var placementKind = PlacementKindFactory.Create((uint) clearedLines.Length, piece.tSpin);
            
            clearedLines.Dispose();

            var nextRen = placementKind.IsLineClear() ? ren + 1 : 0;
            var garbage = (uint) (placementKind.GetGarbage() + (backToBack && placementKind.IsContinuous() ? 1 : 0));

            if (useBag) {
                var nextBag = bag;
                if (nextBag.Take(piece.kind)) {
                    nextBoard.bag = nextBag;
                }
            }

            return new SimpleLockResult(placementKind, pc, nextRen, garbage);
        }

        public SimpleBoard WithHold(PieceKind kind, bool useBag) {
            var newBoard = new SimpleBoard();
            newBoard.ren = ren;
            newBoard.backToBack = backToBack;
            fixed (ushort* cPtr = cells) UnsafeUtility.MemCpy(newBoard.cells, cPtr, sizeof(ushort) * Length);
            
            newBoard.hold = kind;
            if (useBag) {
                var bag = this.bag;
                bag.Take(kind);
                newBoard.bag = bag;
            }

            return newBoard;
        }
    }
}