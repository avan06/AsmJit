using System;

namespace AsmJit.Common.Enums
{
    [Flags]
    internal enum PointerFlags
    {
        None = 0,
        Aligned = 1,
        Protected = 2
    }
}