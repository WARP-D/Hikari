using System;
using Hikari.Puzzle;

namespace Hikari.AI {
    public unsafe struct Move : IComparable<Move> {
        public const int MaxInstructions = 16;
        
        public fixed byte instructions[MaxInstructions];
        public byte length;
        public Piece piece;
        public int time;

        public bool IsFull => length == MaxInstructions;

        public Move Append(Instruction inst, int t, Piece p) {
            if (IsFull) throw new Exception();
            instructions[length++] = (byte) inst;
            time += t;
            piece = p;
            return this;
        }

        public int CompareTo(Move other) {
            var timeComparison = time.CompareTo(other.time);
            if (timeComparison != 0) return timeComparison;
            return length.CompareTo(other.length);
        }

        public Instruction GetInstructionAt(int i) {
            if (i < 0 || i >= length) throw new ArgumentOutOfRangeException();
            return (Instruction) instructions[i];
        }
    }
}