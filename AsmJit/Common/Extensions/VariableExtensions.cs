using AsmJit.Common.Operands;

namespace AsmJit.Common.Extensions
{
    internal static class VariableExtensions
    {
        internal static bool IsGp(this Variable op) => op.RegisterType <= RegisterType.Gpq;

        internal static bool IsGpb(this Variable op) => op.RegisterType <= RegisterType.GpbHi;

        internal static bool IsGpbLo(this Variable op) => op.RegisterType == RegisterType.GpbLo;

        internal static bool IsGpbHi(this Variable op) => op.RegisterType == RegisterType.GpbHi;

        internal static bool IsGpw(this Variable op) => op.RegisterType == RegisterType.Gpw;

        internal static bool IsGpd(this Variable op) => op.RegisterType == RegisterType.Gpd;

        internal static bool IsGpq(this Variable op) => op.RegisterType == RegisterType.Gpq;

        internal static bool IsMm(this Variable op) => op.RegisterType == RegisterType.Mm;

        internal static bool IsK(this Variable op) => op.RegisterType == RegisterType.K;

        internal static bool IsXmm(this Variable op) => op.RegisterType == RegisterType.Xmm;

        internal static bool IsYmm(this Variable op) => op.RegisterType == RegisterType.Ymm;

        internal static bool IsZmm(this Variable op) => op.RegisterType == RegisterType.Zmm;
    }
}
