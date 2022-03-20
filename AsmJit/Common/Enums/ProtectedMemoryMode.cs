using System;

namespace AsmJit.Common.Enums
{
    [Flags]
    internal enum ProtectedMemoryMode
    {
        Writable = 0x00000001,
        Executable = 0x00000002
    }
}