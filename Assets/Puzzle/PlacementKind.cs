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
            return placementKind switch {
                Clear1 => "Single",
                Clear2 => "Double",
                Clear3 => "Triple",
                Clear4 => "Quad",
                TSpin1 => "T-Spin Single",
                TSpin2 => "T-Spin Double",
                TSpin3 => "T-Spin Triple",
                Mini1 => "T-Spin Mini Single",
                Mini2 => "T-Spin Mini Double",
                _ => "..."
            };
        }
    }

    public static class PlacementKindFactory {
        public static PlacementKind Create(uint clearLine, TSpinStatus tSpin) {
            return (tSpin,clearLine) switch {
                (TSpinStatus.None,0) => None,
                (TSpinStatus.None,1) => Clear1,
                (TSpinStatus.None,2) => Clear2,
                (TSpinStatus.None,3) => Clear3,
                (TSpinStatus.None,4) => Clear4,
                (TSpinStatus.Mini,0) => Mini,
                (TSpinStatus.Mini,1) => Mini1,
                (TSpinStatus.Mini,2) => Mini2,
                (TSpinStatus.Full,0) => TSpin,
                (TSpinStatus.Full,1) => TSpin1,
                (TSpinStatus.Full,2) => TSpin2,
                (TSpinStatus.Full,3) => TSpin3,
                _ => throw new ArgumentException()
            };
        }
    }
}