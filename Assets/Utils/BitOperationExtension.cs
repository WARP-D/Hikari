namespace Cyanite.Utils {
    public static class BitOperationExtension {
        public static ushort Shift(this ushort value, int shift) => (ushort) (shift >= 0 ? value << shift : value >> -shift);
        public static int Shift(this int value, int shift) =>  shift >= 0 ? value << shift : value >> -shift;
    }
}