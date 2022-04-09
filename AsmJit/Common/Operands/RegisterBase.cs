using System;

namespace AsmJit.Common.Operands
{
    public abstract class RegisterBase : Operand
    {
        protected RegisterBase(OperandType type) : base(type) { }

        protected RegisterBase(Operand other) : base(other) { }

        internal int Index
        {
            get => Reserved0;
            set => Reserved0 = value;
        }

        internal RegisterType RegisterType
        {
            get => (RegisterType)Reserved1;
            set => Reserved1 = (int)value;
        }

        internal bool IsGp() => RegisterType >= RegisterType.GpbLo && RegisterType <= RegisterType.Gpq;

        internal bool IsGpb() => RegisterType >= RegisterType.GpbLo && RegisterType <= RegisterType.GpbHi;

        internal bool IsGpbLo() => RegisterType == RegisterType.GpbLo;

        internal bool IsGpbHi() => RegisterType == RegisterType.GpbHi;

        internal bool IsGpw() => RegisterType == RegisterType.Gpw;

        internal bool IsGpd() => RegisterType == RegisterType.Gpd;

        internal bool IsGpq() => RegisterType == RegisterType.Gpq;

        internal bool IsMm() => RegisterType == RegisterType.Mm;

        internal bool IsK() => RegisterType == RegisterType.K;

        internal bool IsXmm() => RegisterType == RegisterType.Xmm;

        internal bool IsYmm() => RegisterType == RegisterType.Ymm;

        internal bool IsZmm() => RegisterType == RegisterType.Zmm;
    }
}