using System;

namespace Cyanite.AI {
    public struct ChildrenRef : IEquatable<ChildrenRef> {
        public int start;
        public int length;

        public ChildrenRef(int start, int length) {
            this.start = start;
            this.length = length;
        }

        public bool Equals(ChildrenRef other) {
            return start == other.start && length == other.length;
        }

        public override bool Equals(object obj) {
            return obj is ChildrenRef other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (start * 397) ^ length;
            }
        }

        public static bool operator ==(ChildrenRef left, ChildrenRef right) {
            return left.Equals(right);
        }

        public static bool operator !=(ChildrenRef left, ChildrenRef right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"{start} .. {start + length} ({length})";
        }
    }
}