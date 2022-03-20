using System;

namespace AsmJit.Common.Enums
{
    [Flags]
    public enum AssemblerFeatures
    {
        OptimizedAlign = 0,
        PredictedJumps = 1
    }
}