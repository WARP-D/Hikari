using System;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Mathematics;
using static Hikari.AI.Instruction;
using static Hikari.Puzzle.PieceKind;

namespace Hikari.AI {
    // Currently most of this code is a copy of Cold Clear, thanks to MinusKelvin(@Below0K)
    // But I want to make my original one later
    public static unsafe class NextPlacementsGenerator {
        public static NativeHashMap<Piece,Move> Generate(ref SimpleBoard board, Piece spawned, NativeArray<int4x4> pieceShapes) {
            var columns = stackalloc int[10];
            var maxHeights = stackalloc byte[10];
            board.GetColumns(columns,maxHeights);
            
            var lookup = new NativeHashMap<Piece,Move>(200,Allocator.Temp);
            var founds = new NativeHashMap<Piece,bool>(200,Allocator.Temp);
            var checkQueue = new NativeList<Move>(100,Allocator.Temp);

            
            var maxHeight = 0;
            for (var i = 0; i < 10; i++) {
                if (maxHeights[i] > maxHeight) maxHeight = maxHeights[i];
            }

            if (maxHeight < 16) {
                var maxI = GetMaxStartsIndex(spawned.kind);
                for (var i = 0; i < maxI; i++) {
                    var start = GetStarts(spawned.kind, i);
                    var m1 = start.ToMove();
                    start.Dispose();
                    var originY = start.piece.y;
                    var piece = board.SonicDropFast(start.piece,pieceShapes);
                    m1.piece = piece;
                    CheckAndAddLookup(ref board, ref lookup, m1,pieceShapes);
                    var m2 = m1;
                    var dY = originY - piece.y;
                    m2.instructions[m2.length++] = (byte) SonicDrop;
                    m2.time += dY * 2;
                    checkQueue.Add(m2);
                }
            } else {
                var p = new Piece(spawned.kind);
                var d = board.SonicDropFast(p, pieceShapes);
                var m = new Move();
                m.piece = d;
                m.instructions[0] = (byte) SonicDrop;
                m.length = 1;
                m.time = 2 * (p.y - d.y);
                checkQueue.Add(m);
            }

            bool Next(out Move mv) {
                if (checkQueue.Length == 0) {
                    mv = default;
                    return false;
                }

                checkQueue.Sort();
                mv = checkQueue[0];
                checkQueue.RemoveAtSwapBack(0);
                return true;
            }

            while (Next(out var mv)) {
                if (!mv.IsFull) {
                    Attempt(ref board, mv, ref founds, ref checkQueue, Left, maxHeight, pieceShapes);
                    Attempt(ref board, mv, ref founds, ref checkQueue, Right, maxHeight, pieceShapes);

                    if (mv.piece.kind != O) {
                        Attempt(ref board, mv, ref founds, ref checkQueue, Cw, maxHeight, pieceShapes);
                        Attempt(ref board, mv, ref founds, ref checkQueue, Ccw, maxHeight, pieceShapes);
                    }
                    
                    Attempt(ref board, mv, ref founds, ref checkQueue, Left, maxHeight, pieceShapes, true);
                    Attempt(ref board, mv, ref founds, ref checkQueue, Right, maxHeight, pieceShapes, true);
                    
                    Attempt(ref board, mv, ref founds, ref checkQueue, SonicDrop, maxHeight, pieceShapes);
                }
                
                //Finally add this placement(harddropped) to return array
                var pl = board.SonicDropFast(mv.piece,pieceShapes);
                CheckAndAddLookup(ref board, ref lookup, mv,pieceShapes);
                
            }

            return lookup;
        }

        private static void CheckAndAddLookup(ref SimpleBoard board, ref NativeHashMap<Piece, Move> lookup, Move mv, NativeArray<int4x4> pieceShapes) {
            if (!IsAboveStacking(mv.piece, 20) && !board.CollidesFast(mv.piece,pieceShapes)) {
                lookup.TryAdd(mv.piece, mv);
            }
        }

        private static void Attempt(ref SimpleBoard board, Move move,ref NativeHashMap<Piece,bool> founds,
            ref NativeList<Move> checkQueue, Instruction instruction, int maxBoardHeight, NativeArray<int4x4> pieceShapes, bool repeat = false) {
            if (repeat && !(instruction == Left || instruction == Right)) throw new ArgumentException();
            
            var piece = move.piece;
                
            if (!TryApplyInstruction(ref board, piece, instruction, out var result, pieceShapes)) return;
            piece = result;
            
            var t = 0;
            if (instruction == SonicDrop) {
                t += 2 * move.piece.y - result.y;
            } else {
                t = 1;
            }

            if (move.length != 0 && move.instructions[move.length - 1] == (byte) instruction) {
                t += 1;
            }

            move.Append(instruction, t, result);

            while (repeat && !move.IsFull && TryApplyInstruction(ref board,piece,instruction,out result,pieceShapes)) {
                piece = result;
                move.Append(instruction, 2, result);
            }

            if (result.tSpin != TSpinStatus.None || !IsAboveStacking(result,maxBoardHeight)) {
                // We should do further checks
                if (founds.TryAdd(result, true) && !move.IsFull) {
                    var dropped = board.SonicDropFast(piece, pieceShapes);
                    if (piece.y == dropped.y) move.Append(SonicDrop, 0, dropped);
                    checkQueue.Add(move);
                }
            }
        }

        private static bool TryApplyInstruction(ref SimpleBoard board, Piece piece, Instruction inst, out Piece result, NativeArray<int4x4> pieceShapes) {
            switch (inst) {
                case Left:
                    result = piece.WithOffset(-1, 0);
                    return !board.CollidesFast(result, pieceShapes);
                case Right:
                    result = piece.WithOffset(1, 0);
                    return !board.CollidesFast(result, pieceShapes);
                case Cw:
                    var t = SRSNoAlloc.TryRotate(piece, ref board, true, pieceShapes, out var r1, out var r1Result);
                    result = piece.kind == T ? r1Result.WithTSpinStatus(board.CheckTSpin(r1Result,r1)) : r1Result;
                    return t;
                case Ccw:
                    var s = SRSNoAlloc.TryRotate(piece, ref board, false, pieceShapes, out var r2, out var r2Result);
                    result = piece.kind == T ? r2Result.WithTSpinStatus(board.CheckTSpin(r2Result,r2)) : r2Result;
                    return s;
                case SonicDrop:
                    var originY = piece.y;
                    result = board.SonicDropFast(piece, pieceShapes);
                    return originY == result.y;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inst), inst, null);
            }
        }

        private static bool IsAboveStacking(Piece piece, int maxHeight) {
            if (piece.kind == I) {
                if (piece.spin == 0) return piece.y > maxHeight - 1;
                if (piece.spin == 2) return piece.y > maxHeight - 2;
            } else if (piece.kind == O || piece.spin == 2) {
                return piece.y > maxHeight - 1;
            }

            return piece.y > maxHeight;
        }
        
        private struct Starting : IDisposable {
            public Piece piece;
            public NativeList<Instruction> instructions;
            public int time;

            public Starting(Piece piece, NativeList<Instruction> instructions, int time) {
                this.piece = piece;
                this.instructions = instructions;
                this.time = time;
            }

            public Move ToMove() {
                var m = new Move {
                    piece = piece,
                    time = time
                };
                for (var i = 0; i < instructions.Length; i++) {
                    m.instructions[i] = (byte) instructions[i];
                }

                m.length = (byte) instructions.Length;
                return m;
            }
            
            public Starting(PieceKind kind, sbyte x, sbyte spin, NativeList<Instruction> instructions, int time)
                : this(new Piece(kind,x,19,spin), instructions, time) {}

            public void Dispose() {
                instructions.Dispose();
            }
        }

        private static int GetMaxStartsIndex(PieceKind p) => p == O ? 8 : 33;

        private static Starting GetStarts(PieceKind p, int i, Allocator a = Allocator.Temp) {
            if (p == O) {
                switch (i) {
                    case 0: return new Starting(O,3,0,new NativeList<Instruction>(a), 0);
                    case 1: return new Starting(O,2,0,new NativeList<Instruction>(a) {Left}, 1);
                    case 2: return new Starting(O,4,0,new NativeList<Instruction>(a) {Right}, 1);
                    case 3: return new Starting(O,1,0,new NativeList<Instruction>(a) {Left,Left}, 3);
                    case 4: return new Starting(O,5,0,new NativeList<Instruction>(a) {Right,Right}, 3);
                    case 5: return new Starting(O,0,0,new NativeList<Instruction>(a) {Left,Left,Left}, 5);
                    case 6: return new Starting(O,6,0,new NativeList<Instruction>(a) {Right,Right,Right}, 5);
                    case 7: return new Starting(O,-1,0,new NativeList<Instruction>(a) {Left,Left,Left,Left}, 7);
                    case 8: return new Starting(O,7,0,new NativeList<Instruction>(a) {Right,Right,Right,Right}, 7);
                    default: throw new ArgumentOutOfRangeException();
                }
            } else if (p == I) {
                switch (i) {
                    case 0: return new Starting(I, 3,0,new NativeList<Instruction>(a), 0);
                    case 1: return new Starting(I, 2,0,new NativeList<Instruction>(a) {Left}, 1);
                    case 2: return new Starting(I, 4,0,new NativeList<Instruction>(a) {Right}, 1);
                    case 3: return new Starting(I, 1,0,new NativeList<Instruction>(a) {Left, Left}, 3);
                    case 4: return new Starting(I, 5,0,new NativeList<Instruction>(a) {Right,Right}, 3);
                    case 5: return new Starting(I, 0,0,new NativeList<Instruction>(a) {Left,Left,Left}, 5);
                    case 6: return new Starting(I, 6,0,new NativeList<Instruction>(a) {Right,Right,Right}, 5);
                    
                    case 7: return new Starting(I, 3,1,new NativeList<Instruction>(a) {Cw}, 1);
                    case 8: return new Starting(I, 2,1,new NativeList<Instruction>(a) {Left, Cw}, 2);
                    case 9: return new Starting(I, 4,1,new NativeList<Instruction>(a) {Right, Cw}, 2);
                    case 10: return new Starting(I, 1,1,new NativeList<Instruction>(a) {Left, Cw, Left}, 3);
                    case 11: return new Starting(I, 5,1,new NativeList<Instruction>(a) {Right, Cw,Right}, 3);
                    case 12: return new Starting(I, 0,1,new NativeList<Instruction>(a) {Left, Cw,Left,Left}, 5);
                    case 13: return new Starting(I, 6,1,new NativeList<Instruction>(a) {Right, Cw,Right,Right}, 5);
                    case 14: return new Starting(I, -1,1,new NativeList<Instruction>(a) {Left, Cw,Left,Left,Left}, 7);
                    case 15: return new Starting(I, 7,1,new NativeList<Instruction>(a) {Right, Cw,Right,Right,Right}, 7);
                    case 16: return new Starting(I, -2,1,new NativeList<Instruction>(a) {Left, Cw,Left,Left,Left,Left}, 9);
                    
                    case 17: return new Starting(I, 3,3,new NativeList<Instruction>(a) {Ccw}, 1);
                    case 18: return new Starting(I, 2,3,new NativeList<Instruction>(a) {Left, Ccw}, 2);
                    case 19: return new Starting(I, 4,3,new NativeList<Instruction>(a) {Right, Ccw}, 2);
                    case 20: return new Starting(I, 1,3,new NativeList<Instruction>(a) {Left, Ccw, Left}, 3);
                    case 21: return new Starting(I, 5,3,new NativeList<Instruction>(a) {Right, Ccw,Right}, 3);
                    case 22: return new Starting(I, 0,3,new NativeList<Instruction>(a) {Left, Ccw,Left,Left}, 5);
                    case 23: return new Starting(I, 6,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right}, 5);
                    case 24: return new Starting(I, -1,3,new NativeList<Instruction>(a) {Left, Ccw,Left,Left,Left}, 7);
                    case 25: return new Starting(I, 7,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right,Right}, 7);
                    case 26: return new Starting(I, 8,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right,Right,Right}, 9);
                    
                    case 27: return new Starting(I, 3,2,new NativeList<Instruction>(a) {Cw, Cw}, 3);
                    case 28: return new Starting(I, 2,2,new NativeList<Instruction>(a) {Cw,Left, Cw}, 3);
                    case 29: return new Starting(I, 4,2,new NativeList<Instruction>(a) {Cw, Right, Cw}, 3);
                    case 30: return new Starting(I, 1,2,new NativeList<Instruction>(a) {Cw,Left,Cw, Left}, 4);
                    case 31: return new Starting(I, 5,2,new NativeList<Instruction>(a) {Cw, Right,Cw,Right}, 4);
                    case 32: return new Starting(I, 0,2,new NativeList<Instruction>(a) {Left, Cw,Left,Cw,Left}, 5);
                    case 33: return new Starting(I, 6,2,new NativeList<Instruction>(a) {Right,Cw,Right,Cw,Right}, 5);
                    default: throw new ArgumentOutOfRangeException();
                }
            } else {
                switch (i) {
                    case 0: return new Starting(p, 3,0,new NativeList<Instruction>(a), 0);
                    case 1: return new Starting(p, 2,0,new NativeList<Instruction>(a) {Left}, 1);
                    case 2: return new Starting(p, 4,0,new NativeList<Instruction>(a) {Right}, 1);
                    case 3: return new Starting(p, 1,0,new NativeList<Instruction>(a) {Left, Left}, 3);
                    case 4: return new Starting(p, 5,0,new NativeList<Instruction>(a) {Right,Right}, 3);
                    case 5: return new Starting(p, 0,0,new NativeList<Instruction>(a) {Left,Left,Left}, 5);
                    case 6: return new Starting(p, 6,0,new NativeList<Instruction>(a) {Right,Right,Right}, 5);
                    case 7: return new Starting(p, 7,0,new NativeList<Instruction>(a) {Right,Right,Right,Right}, 7);
                    
                    case 8: return new Starting(p, 3,1,new NativeList<Instruction>(a) {Cw}, 1);
                    case 9: return new Starting(p, 2,1,new NativeList<Instruction>(a) {Left, Cw}, 2);
                    case 10: return new Starting(p, 4,1,new NativeList<Instruction>(a) {Right, Cw}, 2);
                    case 11: return new Starting(p, 1,1,new NativeList<Instruction>(a) {Left, Cw, Left}, 3);
                    case 12: return new Starting(p, 5,1,new NativeList<Instruction>(a) {Right, Cw,Right}, 3);
                    case 13: return new Starting(p, 0,1,new NativeList<Instruction>(a) {Left, Cw,Left,Left}, 5);
                    case 14: return new Starting(p, 6,1,new NativeList<Instruction>(a) {Right, Cw,Right,Right}, 5);
                    case 15: return new Starting(p, -1,1,new NativeList<Instruction>(a) {Left, Cw,Left,Left,Left}, 7);
                    case 16: return new Starting(p, 7,1,new NativeList<Instruction>(a) {Right, Cw,Right,Right,Right}, 7);
                    
                    case 17: return new Starting(p, 3,3,new NativeList<Instruction>(a) {Ccw}, 1);
                    case 18: return new Starting(p, 2,3,new NativeList<Instruction>(a) {Left, Ccw}, 2);
                    case 19: return new Starting(p, 4,3,new NativeList<Instruction>(a) {Right, Ccw}, 2);
                    case 20: return new Starting(p, 1,3,new NativeList<Instruction>(a) {Left, Ccw, Left}, 3);
                    case 21: return new Starting(p, 5,3,new NativeList<Instruction>(a) {Right, Ccw,Right}, 3);
                    case 22: return new Starting(p, 0,3,new NativeList<Instruction>(a) {Left, Ccw,Left,Left}, 5);
                    case 23: return new Starting(p, 6,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right}, 5);
                    case 24: return new Starting(p, 7,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right,Right}, 7);
                    case 25: return new Starting(p, 8,3,new NativeList<Instruction>(a) {Right, Ccw,Right,Right,Right,Right}, 7);
                    
                    case 26: return new Starting(p, 3,2,new NativeList<Instruction>(a) {Cw, Cw}, 3);
                    case 27: return new Starting(p, 2,2,new NativeList<Instruction>(a) {Cw,Left, Cw}, 3);
                    case 28: return new Starting(p, 4,2,new NativeList<Instruction>(a) {Cw, Right, Cw}, 3);
                    case 29: return new Starting(p, 1,2,new NativeList<Instruction>(a) {Cw,Left,Cw, Left}, 4);
                    case 30: return new Starting(p, 5,2,new NativeList<Instruction>(a) {Cw, Right,Cw,Right}, 4);
                    case 31: return new Starting(p, 0,2,new NativeList<Instruction>(a) {Left, Cw,Left,Cw,Left}, 5);
                    case 32: return new Starting(p, 6,2,new NativeList<Instruction>(a) {Right,Cw,Right,Cw,Right}, 5);
                    case 33: return new Starting(p, 7,2,new NativeList<Instruction>(a) {Right,Cw,Right,Cw,Right, Right}, 7);
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}