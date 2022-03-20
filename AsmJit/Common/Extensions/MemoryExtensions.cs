using AsmJit.Common.Operands;

namespace AsmJit.Common.Extensions
{
    internal static class MemoryExtensions
    {
        internal static bool IsBaseIndexType(this Memory op) => op.MemoryType <= MemoryType.StackIndex;

        internal static bool HasBaseOrIndex(this Memory op) => op.Base != RegisterIndex.Invalid || op.Index != RegisterIndex.Invalid;
    }
}
