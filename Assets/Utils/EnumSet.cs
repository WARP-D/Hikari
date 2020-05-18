using System;

namespace Cyanite.Utils {
    public struct EnumSet<T> where T : Enum {
        private int flags;
    }
}