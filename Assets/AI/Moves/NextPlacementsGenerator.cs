using System;
using System.Runtime.CompilerServices;
using Hikari.AI.Utils.Collection;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Mathematics;
using static Hikari.AI.Instruction;
using static Hikari.Puzzle.PieceKind;

namespace Hikari.AI {
    // Currently most of this code is a copy of Cold Clear, thanks to MinusKelvin(@Below0K)
    // But I want to make my original one later
    public static unsafe class NextPlacementsGenerator {
        public static NativeHashMap<Piece, Move> Generate(ref SimpleBoard board, Piece spawned,
            NativeArray<int4x4> pieceShapes, bool holdUse) {
            var columns = stackalloc int[10];
            var maxHeights = stackalloc byte[10];
            board.GetColumns(columns, maxHeights);

            var lookup = new NativeHashMap<Piece, Move>(200, Allocator.Temp);
            var passed = new NativeHashMap<Piece, bool>(200, Allocator.Temp);
            var checkQueue = new NativePriorityQueue<Move>(false, 100, Allocator.Temp);


            var maxHeight = 0;
            for (var i = 0; i < 10; i++) {
                if (maxHeights[i] > maxHeight) maxHeight = maxHeights[i];
            }

            if (maxHeight < 16) {
                var starts = GenerateStarts(spawned.kind, holdUse);
                for (var i = 0; i < starts.Length; i++) {
                    var start = starts[i];
                    var originY = start.piece.y;
                    var piece = board.SonicDropFast(start.piece, pieceShapes);
                    start.piece = piece;
                    Confirm(ref board, ref lookup, start, pieceShapes);
                    var m2 = start;
                    var dY = originY - piece.y;
                    m2.instructions[m2.length++] = (byte) SonicDrop;
                    m2.time += dY * 2;
                    checkQueue.Enqueue(m2);
                }
            
                starts.Dispose();
            } else {
                var p = new Piece(spawned.kind);
                var d = board.SonicDropFast(p, pieceShapes);
                var m = new Move();
                m.hold = holdUse;
                m.piece = d;
                m.instructions[0] = (byte) SonicDrop;
                m.length = 1;
                m.time = 2 * (p.y - d.y);
                checkQueue.Enqueue(m);
            }

            bool Next(out Move mv) {
                if (checkQueue.TryDequeue(out var next)) {
                    mv = next;
                    return true;
                } else {
                    mv = default;
                    return false;
                }
            }

            while (Next(out var mv)) {
                if (!mv.IsFull) {
                    Attempt(ref board, mv, ref passed, ref checkQueue, Left, maxHeight, pieceShapes);
                    Attempt(ref board, mv, ref passed, ref checkQueue, Right, maxHeight, pieceShapes);

                    if (mv.piece.kind != O) {
                        Attempt(ref board, mv, ref passed, ref checkQueue, Cw, maxHeight, pieceShapes);
                        Attempt(ref board, mv, ref passed, ref checkQueue, Ccw, maxHeight, pieceShapes);
                    }

                    Attempt(ref board, mv, ref passed, ref checkQueue, Left, maxHeight, pieceShapes, true);
                    Attempt(ref board, mv, ref passed, ref checkQueue, Right, maxHeight, pieceShapes, true);

                    Attempt(ref board, mv, ref passed, ref checkQueue, SonicDrop, maxHeight, pieceShapes);
                }

                //Finally add this placement(harddropped) to return array
                var pl = board.SonicDropFast(mv.piece, pieceShapes);
                var mHard = mv;
                mHard.piece = pl;
                Confirm(ref board, ref lookup, mHard, pieceShapes);
            }

            passed.Dispose();
            checkQueue.Dispose();
            return lookup;
        }

        private static void Confirm(ref SimpleBoard board, ref NativeHashMap<Piece, Move> lookup, Move mv,
            NativeArray<int4x4> pieceShapes) {
            if (!IsAboveStacking(mv.piece, 20) && !board.CollidesFast(mv.piece, pieceShapes)) {
                lookup.TryAdd(mv.piece, mv);
            }
        }

        private static void Attempt(ref SimpleBoard board, Move move, ref NativeHashMap<Piece, bool> alreadyPassed,
            ref NativePriorityQueue<Move> checkQueue, Instruction instruction, int maxBoardHeight,
            NativeArray<int4x4> pieceShapes, bool repeat = false) {
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

            if (move.length != 0 && move.GetInstructionAt(move.length - 1) == instruction) {
                t += 1;
            }

            move.Append(instruction, t, result);

            while (repeat && !move.IsFull &&
                   TryApplyInstruction(ref board, piece, instruction, out result, pieceShapes)) {
                piece = result;
                move.Append(instruction, 2, result);
            }

            if (result.tSpin != TSpinStatus.None || !IsAboveStacking(result, maxBoardHeight)) {
                // We should do further checks
                if (alreadyPassed.TryAdd(result, true) && !move.IsFull) {
                    var dropped = board.SonicDropFast(piece, pieceShapes);
                    if (piece.y != dropped.y) move.Append(SonicDrop, (piece.y - dropped.y) * 2, dropped);
                    checkQueue.Enqueue(move);
                }
            }
        }

        private static bool TryApplyInstruction(ref SimpleBoard board, Piece piece, Instruction inst, out Piece result,
            NativeArray<int4x4> pieceShapes) {
            switch (inst) {
                case Left:
                    result = piece.WithOffset(-1, 0);
                    return !board.CollidesFast(result, pieceShapes);
                case Right:
                    result = piece.WithOffset(1, 0);
                    return !board.CollidesFast(result, pieceShapes);
                case Cw:
                    var t = SRSNoAlloc.TryRotate(piece, ref board, true, pieceShapes, out var r1, out var r1Result);
                    result = piece.kind == T ? r1Result.WithTSpinStatus(board.CheckTSpin(r1Result, r1)) : r1Result;
                    return t;
                case Ccw:
                    var s = SRSNoAlloc.TryRotate(piece, ref board, false, pieceShapes, out var r2, out var r2Result);
                    result = piece.kind == T ? r2Result.WithTSpinStatus(board.CheckTSpin(r2Result, r2)) : r2Result;
                    return s;
                case SonicDrop:
                    var originY = piece.y;
                    result = board.SonicDropFast(piece, pieceShapes);
                    return originY != result.y;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inst), inst, null);
            }
        }

        private static bool IsAboveStacking(Piece piece, int maxHeight) {
            if (piece.kind == I) {
                if (piece.spin == 0) return piece.y > maxHeight - 2;
                if (piece.spin == 2) return piece.y > maxHeight - 1;
            } else if (piece.kind == O || piece.spin == 0) {
                return piece.y > maxHeight - 1;
            }

            return piece.y > maxHeight;
        }

        private static NativeArray<Move> GenerateStarts(PieceKind p, bool holdUsed) {
            var moves = new NativeArray<Move>(p == O ? 9 : 34, Allocator.Temp);
            if (p == O) {
                for (sbyte x = -1; x < 8; x++) {
                    var mv = moves[x + 1];
                    mv.hold = holdUsed;
                    mv.piece = new Piece(O);
                    MoveTo(x, 0, ref mv);
                    moves[x + 1] = mv;
                }
            } else if (p == I) {
                for (sbyte x = 0; x < 7; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(I)
                    };
                    MoveTo(x, 0, ref mv);
                    moves[x] = mv;
                }

                for (sbyte x = -2; x < 8; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(I)
                    };
                    MoveTo(x, 1, ref mv);
                    moves[7 + x + 2] = mv;
                }

                for (sbyte x = -1; x < 9; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(I)
                    };
                    MoveTo(x, 3, ref mv);
                    moves[7 + 10 + x + 1] = mv;
                }

                for (sbyte x = 0; x < 7; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(I)
                    };
                    MoveTo(x, 2, ref mv);
                    moves[7 + 10 + 10 + x] = mv;
                }
            } else {
                for (sbyte x = 0; x < 8; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(p)
                    };
                    MoveTo(x, 0, ref mv);
                    moves[x] = mv;
                }

                for (sbyte x = -1; x < 8; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(p)
                    };
                    MoveTo(x, 1, ref mv);
                    moves[8 + x + 1] = mv;
                }

                for (sbyte x = 0; x < 9; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(p)
                    };
                    MoveTo(x, 3, ref mv);
                    moves[8 + 9 + x] = mv;
                }

                for (sbyte x = 0; x < 8; x++) {
                    var mv = new Move {
                        hold = holdUsed,
                        piece = new Piece(p)
                    };
                    MoveTo(x, 2, ref mv);
                    moves[8 + 9 + 9 + x] = mv;
                }
            }

            return moves;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveTo(sbyte to, sbyte r, ref Move mv) {
            var c = 0;
            for (var i = 0; i < 3 - to; i++) {
                mv.Append(Left, i == 0 ? 1 : 2, mv.piece.WithOffset(-1, 0));
                if (r != c) {
                    c += r == -1 ? -1 : 1;
                    mv.Append(r == -1 ? Ccw : Cw, 0, mv.piece.WithSpin((sbyte) c));
                }
            }

            for (var i = 0; i < to - 3; i++) {
                mv.Append(Right, i == 0 ? 1 : 2, mv.piece.WithOffset(1, 0));
                if (r != c) {
                    c += r == -1 ? -1 : 1;
                    mv.Append(r == -1 ? Ccw : Cw, 0, mv.piece.WithSpin((sbyte) c));
                }
            }

            while (r != c) {
                c += r == -1 ? -1 : 1;
                mv.Append(r == -1 ? Ccw : Cw, mv.length == 0 ? 1 : 2, mv.piece.WithSpin((sbyte) c));
            }
        }
    }
}