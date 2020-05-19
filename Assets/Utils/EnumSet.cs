using System;

namespace Hikari.Utils {
    public struct EnumSet<T> where T : Enum {
        private int flags;
    }
}