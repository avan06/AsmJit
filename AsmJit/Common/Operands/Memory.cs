using System;

namespace AsmJit.Common.Operands
{
    public class Memory : Operand
    {
        internal Memory() : this(MemoryType.BaseIndex) => Base = RegisterIndex.Invalid;

        internal Memory(Memory other) : base(other) { }

        internal Memory(Memory other, int size) : base(other) => Size = size;

        internal Memory(MemoryType memType) : this(memType, 0, 0) { }

        internal Memory(MemoryType memType, int disp, int size) : this(memType, null, disp, size) { }

        internal Memory(MemoryType memType, GpVariable @base, int disp, int size) : this(memType, @base, null, 0, disp, size) { }

        internal Memory(MemoryType memType, GpVariable @base, GpVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            MemoryType = memType;
            Displacement = disp;
            if (@base != null) Id = @base.Id;
            if (index != null)
            {
                Index = index.Id;
                Flags = shift << Constants.X86.MemShiftIndex;
            }
            else Index = RegisterIndex.Invalid;
        }

        internal Memory(MemoryType memType, RegisterBase index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            MemoryType = memType;
            Displacement = disp;
            Index = index.Index;
            var flags = shift << Constants.X86.MemShiftIndex;

            if (index.IsGp()) flags |= GetGpdFlags(index);
            else if (index.IsXmm()) flags |= Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex;
            else if (index.IsYmm()) flags |= Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex;

            Flags = flags;
        }
        internal Memory(Variable index, int shift, int disp, int size) : this(MemoryType.Absolute, index, shift, disp, size) => Index = index.Id;

        internal Memory(Operand @base, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Displacement = disp;
            Index = RegisterIndex.Invalid;
            if (@base is RipRegister)
            {
                MemoryType = MemoryType.Rip;
                return;
            }
            Id = @base.Id;

            switch (@base)
            {
                case Label _:
                    MemoryType = MemoryType.Label;
                    break;
                case GpRegister _:
                case GpVariable _:
                    if (@base is GpRegister regBase) Id = regBase.Index;
                    MemoryType = MemoryType.BaseIndex;
                    Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibGpz << Constants.X86.MemVSibIndex);
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        internal Memory(Operand @base, RegisterBase index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Displacement = disp;
            if (@base is Label)
            {
                Id = @base.Id;
                MemoryType = MemoryType.Label;
                if (index is GpRegister rIndex)
                {
                    Index = rIndex.Index;
                    Flags = (Constants.X86.MemVSibGpz << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
                }
                else if (index is GpVariable)
                {
                    Index = index.Id;
                    Flags = GetGpdFlags(index) + (shift << Constants.X86.MemShiftIndex);
                }
                else throw new ArgumentException();
                return;
            }
            else if ((!(@base is GpRegister) || !(index is Register)) && (!(@base is GpVariable) || !(index is Variable))) throw new ArgumentException();

            MemoryType = MemoryType.BaseIndex;
            Flags = GetGpdFlags(@base) + (shift << Constants.X86.MemShiftIndex);

            if (index is Register)
            {
                Id = ((Register)@base).Index;
                Index = index.Index;
            }
            else if (index is Variable)
            {
                Id = @base.Id;
                Index = index.Id;
                if (index is XmmVariable || index is YmmVariable) MemoryType = MemoryType.Label;
            }
            if (index.IsXmm()) Flags += (Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex);
            else if (index.IsYmm()) Flags += (Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex);
        }

        private int GetGpdFlags(Operand @base) => (@base.Size & 4) << (Constants.X86.MemGpdIndex - 2);

        internal void SetGpdBase(int b) => Flags = (Flags & Constants.X86.MemGpdMask) + (b << Constants.X86.MemGpdIndex);

        internal Memory Adjust(int disp)
        {
            Displacement += disp;
            return this;
        }

        internal int Base
        {
            get => Id;
            set => Id = value;
        }

        internal MemoryType MemoryType
        {
            get => (MemoryType)Reserved0;
            set => Reserved0 = (int)value;
        }

        internal int Flags
        {
            get => Reserved1;
            set => Reserved1 = value;
        }

        internal int Index
        {
            get => Reserved2;
            set => Reserved2 = value;
        }

        internal int Displacement
        {
            get => Reserved3;
            set => Reserved3 = value;
        }

        public long VSib
        {
            get => (Flags >> Constants.X86.MemVSibIndex) & Constants.X86.MemVSibBits;
            set => Flags = (int)((Flags & Constants.X86.MemVSibMask) + (value << Constants.X86.MemVSibIndex));
        }

        public long Segment
        {
            get => (Flags >> Constants.X86.MemSegIndex) & Constants.X86.MemSegBits;
            set => Flags = (int)((Flags & Constants.X86.MemSegMask) + (value << Constants.X86.MemSegIndex));
        }

        public long Shift
        {
            get => (Flags >> Constants.X86.MemShiftIndex) & Constants.X86.MemShiftBits;
            set => Flags = (int)((Flags & Constants.X86.MemShiftMask) + (value << Constants.X86.MemShiftIndex));
        }

        public bool HasSegment => (Flags & Constants.X86.MemSegMask) != Constants.X86.SegDefault << Constants.X86.MemSegIndex;

        public bool HasGdpBase => (Flags & Constants.X86.MemGpdMask) != 0;

        internal bool IsBaseIndexType() => MemoryType <= MemoryType.StackIndex;

        internal bool HasBaseOrIndex() => Base != RegisterIndex.Invalid || Index != RegisterIndex.Invalid;

        /// <summary>
        /// Creates `[base + disp]` memory operand.
        /// </summary>
        public static Memory Ptr(Operand @base, int disp = 0, int size = 0) => new Memory(@base, disp, size);
        public static Memory Byte(Operand @base, int disp = 0) => Ptr(@base, disp, 1);
        public static Memory Word(Operand @base, int disp = 0) => Ptr(@base, disp, 2);
        public static Memory DWord(Operand @base, int disp = 0) => Ptr(@base, disp, 4);
        public static Memory QWord(Operand @base, int disp = 0) => Ptr(@base, disp, 8);
        public static Memory TWord(Operand @base, int disp = 0) => Ptr(@base, disp, 10);
        public static Memory OWordPtr(Operand @base, int disp = 0) => Ptr(@base, disp, 16);
        public static Memory YWordPtr(Operand @base, int disp = 0) => Ptr(@base, disp, 32);
        public static Memory ZWordPtr(Operand @base, int disp = 0) => Ptr(@base, disp, 64);

        /// <summary>
        /// Creates `[base + (index << shift) + disp]` memory operand (scalar&vector index).
        /// </summary>
        public static Memory Ptr(Operand @base, Operand index, int shift = 0, int disp = 0, int size = 0)
        {
            if (@base is Label)
            {
                if (!(index is GpRegister) && !(index is GpVariable)) throw new ArgumentException();
            }
            else if (!(@base is GpRegister) && !(@base is GpVariable)) throw new ArgumentException();

            if (!(index is Register) && !(index is Variable)) throw new ArgumentException();

            return new Memory(@base, (dynamic)index, shift, disp, size);
        }
        public static Memory Byte(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 1);
        public static Memory Word(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 2);
        public static Memory DWord(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 4);
        public static Memory QWord(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 8);
        public static Memory TWord(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 10);
        public static Memory OWordPtr(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 16);
        public static Memory YWordPtr(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 32);
        public static Memory ZWordPtr(Operand @base, Operand index, int shift = 0, int disp = 0) => Ptr(@base, index, shift, disp, 64);

        /// <summary>
        /// Creates `[base + (index << shift) + disp]` absolute memory operand.
        /// </summary>
        public static Memory PtrAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0, int size = 0)
        {
            switch (index)
            {
                case null:
                    return new Memory(MemoryType.Absolute, (int)(pAbs + disp), size);
                case GpRegister _:
                case XmmRegister _:
                case YmmRegister _:
                    return new Memory(MemoryType.Absolute, (Register)index, shift, (int)(pAbs + disp), size);
                case GpVariable _:
                case XmmVariable _:
                case YmmVariable _:
                    return new Memory((Variable)index, shift, (int)(pAbs + disp), size);
                default:
                    throw new ArgumentException();
            }
        }
        internal static Memory PtrAbs(long @base, int size = 0) => PtrAbs((IntPtr)(@base >> 32), null, 0, (int)(@base & 0xFFFFFFFFu), size);
        public static Memory ByteAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 1);
        public static Memory WordAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 2);
        public static Memory DWordAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 4);
        public static Memory QWordAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 8);
        public static Memory TWordPtrAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 10);
        public static Memory OWordPtrAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 16);
        public static Memory YWordPtrAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 32);
        public static Memory ZWordPtrAbs(IntPtr pAbs, Operand index = null, int shift = 0, int disp = 0) => PtrAbs(pAbs, index, shift, disp, 64);
    }
}