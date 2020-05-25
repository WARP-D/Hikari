using Hikari.Puzzle;

namespace Hikari.AI {
    public readonly struct SimpleLockResult {
        public readonly PlacementKind placementKind;
        public readonly bool b2b;
        public readonly bool perfectClear;
        public readonly uint ren;
        public readonly uint attack;

        public SimpleLockResult(PlacementKind placementKind, bool perfectClear, uint ren, uint attack) {
            this.placementKind = placementKind;
            b2b = placementKind.IsContinuous();
            this.perfectClear = perfectClear;
            this.ren = ren;
            this.attack = attack;
        }
    }
}