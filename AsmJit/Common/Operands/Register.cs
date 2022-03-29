using System;

namespace AsmJit.Common.Operands
{
    public abstract class Register : Operand
    {
        protected Register() : base(OperandType.Register)
        {
            Index = RegisterIndex.Invalid;
            RegisterType = RegisterType.Invalid;
        }

        protected Register(Register other) : base(other) { }

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

        internal static Register FromVariable(Variable var, int index)
        {
            switch (var.RegisterType)
            {
                case RegisterType.GpbLo:
                    return new GpRegister(GpRegisterType.GpbLo, index) { Id = var.Id, Size = var.Size };
                case RegisterType.GpbHi:
                    return new GpRegister(GpRegisterType.GpbHi, index) { Id = var.Id, Size = var.Size };
                case RegisterType.Gpw:
                    return new GpRegister(GpRegisterType.Gpw, index) { Id = var.Id, Size = var.Size };
                case RegisterType.Gpd:
                    return new GpRegister(GpRegisterType.Gpd, index) { Id = var.Id, Size = var.Size };
                case RegisterType.Gpq:
                    return new GpRegister(GpRegisterType.Gpq, index) { Id = var.Id, Size = var.Size };
                case RegisterType.Fp:
                    return new FpRegister(index) { Id = var.Id, Size = var.Size };
                case RegisterType.Mm:
                    return new MmRegister(index) { Id = var.Id, Size = var.Size };
                case RegisterType.K:
                    return new KRegister(index) { Id = var.Id, Size = var.Size };
                case RegisterType.Xmm:
                    return new XmmRegister(index) {Id = var.Id, Size = var.Size};
                case RegisterType.Ymm:
                    return new YmmRegister(index) { Id = var.Id, Size = var.Size };
                case RegisterType.Zmm:
                    return new ZmmRegister(index) { Id = var.Id, Size = var.Size };
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        public override string ToString() => string.Format("[{0}: Id={1}, Size={2}, Type={3}, Idx={4}]", OperandType, Id == Constants.InvalidId ? "#" : Id.ToString(), Size, RegisterType, Index == RegisterIndex.Invalid ? "#" : Index.ToString());
    }
}