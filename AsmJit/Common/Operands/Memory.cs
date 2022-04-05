using System;
using AsmJit.Common.Extensions;

namespace AsmJit.Common.Operands
{
    public class Memory : Operand
    {
        internal Memory() : base(OperandType.Memory)
        {
            MemoryType = MemoryType.BaseIndex;
            Displacement = 0;
            Index = RegisterIndex.Invalid;
            Base = RegisterIndex.Invalid;
        }

        internal Memory(Memory other) : base(other) { }

        internal Memory(Memory other, int size) : base(other) => Size = size;

        internal Memory(RipRegister rip, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            MemoryType = MemoryType.Rip;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
        }

        internal Memory(MemoryType memType) : base(OperandType.Memory)
        {
            MemoryType = memType;
            Displacement = 0;
            Index = RegisterIndex.Invalid;
        }

        internal Memory(MemoryType memType, GpVariable @base, int disp, int size) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = memType;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
        }

        internal Memory(MemoryType memType, GpVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            MemoryType = memType;
            Displacement = disp;
            Index = index.RegisterIndex;

            var flags = shift << Constants.X86.MemShiftIndex;
            if (index.IsGp()) flags |= GetGpdFlags(index);
            else if (index.IsXmm()) flags |= Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex;
            else if (index.IsYmm()) flags |= Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex;
            Flags = flags;
        }

        internal Memory(MemoryType memType, GpVariable @base, GpVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = memType;
            Displacement = disp;
            Index = index.Id;
            Flags = shift << Constants.X86.MemShiftIndex;
        }
        
        ////////////////////////////////////
        internal Memory(GpRegister @base, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Index;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibGpz << Constants.X86.MemVSibIndex);
        }

        internal Memory(GpRegister @base, GpRegister index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Index;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = index.Index;
            Flags = GetGpdFlags(@base) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(GpRegister @base, XmmRegister index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Index;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = index.Index;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(GpRegister @base, YmmRegister index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Index;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = index.Index;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(Label label, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = label.Id;
            MemoryType = MemoryType.Label;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
        }

        internal Memory(Label label, GpRegister index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = label.Id;
            MemoryType = MemoryType.Label;
            Displacement = disp;
            Index = index.Index;
            Flags = (Constants.X86.MemVSibGpz << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(Label label, GpVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = label.Id;
            MemoryType = MemoryType.Label;
            Displacement = disp;
            Index = index.Id;
            Flags = GetGpdFlags(index) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(GpVariable @base, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibGpz << Constants.X86.MemVSibIndex);
        }

        internal Memory(GpVariable @base, GpVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = MemoryType.BaseIndex;
            Displacement = disp;
            Index = index.Id;
            Flags = GetGpdFlags(@base) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(GpVariable @base, XmmVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = MemoryType.Label;
            Displacement = disp;
            Index = index.Id;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(GpVariable @base, YmmVariable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            Id = @base.Id;
            MemoryType = MemoryType.Label;
            Displacement = disp;
            Index = index.Id;
            Flags = GetGpdFlags(@base) + (Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex) + (shift << Constants.X86.MemShiftIndex);
        }

        internal Memory(MemoryType memType, int disp, int size) : base(OperandType.Memory, size)
        {
            MemoryType = memType;
            Displacement = disp;
            Index = RegisterIndex.Invalid;
        }

        internal Memory(MemoryType memType, Register index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
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

        internal Memory(Variable index, int shift, int disp, int size = 0) : base(OperandType.Memory, size)
        {
            MemoryType = MemoryType.Absolute;
            Displacement = disp;
            Index = index.Id;

            var flags = shift << Constants.X86.MemShiftIndex;
            var indexRegType = index.RegisterType;
            if (indexRegType <= RegisterType.Gpq) flags |= GetGpdFlags(index);
            else switch (indexRegType)
                {
                    case RegisterType.Xmm:
                        flags |= Constants.X86.MemVSibXmm << Constants.X86.MemVSibIndex;
                        break;
                    case RegisterType.Ymm:
                        flags |= Constants.X86.MemVSibYmm << Constants.X86.MemVSibIndex;
                        break;
                }
            Flags = flags;
        }
        ////////////////////////////////////

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
        public static Memory Ptr(Operand @base, int disp = 0, int size = 0)
        {
            dynamic caseBase;
            if (@base is Label label) caseBase = label;
            else if (@base is GpRegister regBase) caseBase = regBase;
            else if (@base is RipRegister ripRegister) caseBase = ripRegister;
            else if (@base is GpVariable gpVariable) caseBase = gpVariable;
            else throw new ArgumentException();

            return new Memory(caseBase, disp, size);
        }
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
        public static Memory Ptr(Operand rawBase, Operand rawIndex, int shift = 0, int disp = 0, int size = 0)
        {
            dynamic @base;
            dynamic index;
            bool isLabel = false;
            if (rawBase is Label label)
            {
                @base = label;
                isLabel = true;
            }
            else if (rawBase is GpRegister regBase) @base = regBase;
            else if (rawBase is GpVariable varBase) @base = varBase;
            else throw new ArgumentException();

            if (isLabel && !(rawIndex is GpRegister) && !(rawIndex is GpVariable)) throw new ArgumentException();

            else if (rawIndex is GpRegister gpRegister) index = gpRegister;
            else if (rawIndex is XmmRegister xmmRegister) index = xmmRegister;
            else if (rawIndex is YmmRegister ymmRegister) index = ymmRegister;
            else if (rawIndex is GpVariable gpVariable) index = gpVariable;
            else if (rawIndex is XmmVariable xmmVariable) index = xmmVariable;
            else if (rawIndex is YmmVariable ymmVariable) index = ymmVariable;
            else throw new ArgumentException();

            return new Memory(@base, index, shift, disp, size);
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
        public static Memory PtrAbs(IntPtr pAbs, Operand rawIndex = null, int shift = 0, int disp = 0, int size = 0)
        {
            dynamic index = rawIndex;
            switch (rawIndex)
            {
                case null:
                    return new Memory(MemoryType.Absolute, (int)(pAbs + disp), size);
                case GpRegister _:
                case XmmRegister _:
                case YmmRegister _:
                    return new Memory(MemoryType.Absolute, index, shift, (int)(pAbs + disp), size);
                case GpVariable _:
                case XmmVariable _:
                case YmmVariable _:
                    return new Memory(index, (int)(pAbs + disp), shift, size);
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