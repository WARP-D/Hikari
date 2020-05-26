using System;
using static Hikari.Puzzle.PlacementKind;

namespace Hikari.Puzzle {
    public enum PlacementKind : byte {
        None,
        Clear1,
        Clear2,
        Clear3,
        Clear4,
        Mini,
        Mini1,
        Mini2,
        TSpin,
        TSpin1,
        TSpin2,
        TSpin3
    }

    public static class PlacementKindExtensions {
        public static uint GetGarbage(this PlacementKind placementKind) {
            switch (placementKind) {
                case Clear2:
                case Mini2:
                    return 1;
                case Clear3:
                case TSpin1:
                    return 2;
                case Clear4:
                case TSpin2:
                    return 4;
                case TSpin3:
                    return 6;
                default:
                    return 0;
            }
        }

        public static bool IsContinuous(this PlacementKind placementKind) {
            switch (placementKind) {
                case Mini:
                case Mini1:
                case Mini2:
                case TSpin:
                case TSpin1:
                case TSpin2:
                case TSpin3:
                case Clear4:
                    return true;
                default: return false;
            }
        }

        public static bool IsLineClear(this PlacementKind placementKind) {
            switch (placementKind) {
                case None:
                case Mini:
                case TSpin:
                    return false;
                default:
                    return true;
            }
        }

        public static string GetFullName(this PlacementKind placementKind) {
            switch (placementKind) {
                case Clear1: return "Single";
                case Clear2: return "Double";
                case Clear3: return "Triple";
                case Clear4: return "Quad";
                case TSpin1: return "T-Spin Single";
                case TSpin2: return "T-Spin Double";
                case TSpin3: return "T-Spin Triple";
                case Mini1: return "T-Spin Mini Single";
                case Mini2: return "T-Spin Mini Double";
                default: return "...";
            }
        }
    }

    public static class PlacementKindFactory {
        public static PlacementKind Create(uint clearLine, TSpinStatus tSpin) {
            switch (tSpin) {
                case TSpinStatus.None:
                    switch (clearLine) {
                        case 0: return None;
                        case 1: return Clear1;
                        case 2: return Clear2;
                        case 3: return Clear3;
                        case 4: return Clear4;
                        default: throw new ArgumentOutOfRangeException();
                    }
                case TSpinStatus.Mini:
                    switch (clearLine) {
                        case 0: return Mini;
                        case 1: return Mini1;
                        case 2: return Mini2;
                        default: throw new ArgumentOutOfRangeException();
                    }
                case TSpinStatus.Full:
                    switch (clearLine) {
                        case 0: return TSpin;
                        case 1: return TSpin1;
                        case 2: return TSpin2;
                        case 3: return TSpin3;
                        default: throw new ArgumentOutOfRangeException();
                    }
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}