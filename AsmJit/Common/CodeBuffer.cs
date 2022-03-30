﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;

namespace AsmJit.Common
{
    internal class CodeBuffer
    {
        private const int MemAllocGrowMax = 8192 * 1024;

        private static readonly int MemAllocOverhead = 4 * (Constants.X64 ? 8 : 4);

        private class EmitContextData
        {
            internal long OpCode;
            internal long ImmediateValue;
            internal int ImmediateLength;
            internal int Base;
            internal int Index;
            internal LabelData Label;
            internal int DisplacementOffset;
            internal int DisplacementSize;
            internal int RelocationId;
            internal int ModRmRegister;
            internal Memory ModRmMemory;
            internal long Operand;
            internal InstructionOptions InstructionOptions;
            internal Operand Operand0 { get; private set; }
            internal Operand Operand1 { get; private set; }
            internal Operand Operand2 { get; private set; }
            internal Operand Operand3 { get; private set; }
            internal long SecondaryOpCode { get; private set; }

            internal void Init(long primaryOpCode, long secondaryOpCode, InstructionOptions instructionOptions, Operand op0, Operand op1, Operand op2, Operand op3)
            {
                Operand0 = op0;
                Operand1 = op1;
                Operand2 = op2;
                Operand3 = op3;

                InstructionOptions = instructionOptions;
                OpCode = primaryOpCode;
                SecondaryOpCode = secondaryOpCode;
                ImmediateValue = 0;
                ImmediateLength = 0;
                Base = -1;
                Index = -1;
                Label = null;
                DisplacementOffset = 0;
                DisplacementSize = 0;
                RelocationId = -1;
                ModRmRegister = -1;
                ModRmMemory = null;
                Operand = ExtractO(primaryOpCode);
            }
        }

        private enum RelocationMode
        {
            AbsToAbs = 0,
            RelToAbs = 1,
            AbsToRel = 2,
            Trampoline = 3
        }

        private sealed class RelocationData
        {
            internal Pointer Data;
            internal Pointer From;
            internal int Size;
            internal RelocationMode Mode;
        }

        private class OpCodeMm
        {
            internal readonly int Length;
            internal readonly byte[] Data;

            internal OpCodeMm(int length = 0, params byte[] data)
            {
                if (data == null || data.Length == 0) data = new byte[] { 0x00, 0x00, 0 };
                Length = length;
                Data = data;
            }
        }

        private static OpCodeMm[] _opCodeMm =
        {
            new OpCodeMm(),
            new OpCodeMm(1, 0x0F, 0x00, 0),
            new OpCodeMm(2, 0x0F, 0x38, 0),
            new OpCodeMm(2, 0x0F, 0x3A, 0),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(),
            new OpCodeMm(0, 0x0F, 0x01, 0)
        };

        private static byte[] _opCodePp = { 0x00, 0x66, 0xF3, 0xF2, 0x00, 0x00, 0x00, 0x9B };

        private static Register[] _patchedHiRegs =
        {
            Cpu.GpbHi(4),
            Cpu.GpbHi(5),
            Cpu.GpbHi(6),
            Cpu.GpbHi(7)
        };

        private static byte[] _segmentPrefix = { 0x00, 0x26, 0x2E, 0x36, 0x3E, 0x64, 0x65 };
        private static byte[] _opCodePushSeg = { 0x00, 0x06, 0x0E, 0x16, 0x1E, 0xA0, 0xA8 };
        private static byte[] _opCodePopSeg = { 0x00, 0x07, 0x00, 0x17, 0x1F, 0xA1, 0xA9 };

        private EmitContextData _eh = new EmitContextData();
        private AssemblerBase _assemblerBase;
        private Pointer _buffer;
        private Pointer _end;
        private Pointer _cursor;
        private int _trampolinesSize;
        private List<RelocationData> _relocations = new List<RelocationData>();
        private LabelLink _unusedLinks;

        public CodeBuffer(AssemblerBase assemblerBase) => _assemblerBase = assemblerBase;

        protected int Capacity => (int)(_end - _buffer);

        protected int Offset => (int)(_cursor - _buffer);

        protected int TrampolinesSize => _trampolinesSize;

        protected int CodeSize => Offset + _trampolinesSize;

        protected int RemainingSpace => (int)(_end - _cursor);

        internal void Align(AligningMode alignMode, int offset)
        {
            if (alignMode > AligningMode.Zero || offset <= 1 || !offset.IsPowerOf2() || offset > 64) throw new ArgumentException();
            var i = Offset.AlignDiff(offset);
            if (i == 0) return;
            if (RemainingSpace < i) Grow(i);
            //var cursor = _cursor;
            var pattern = 0x00;

            switch (alignMode)
            {
                case AligningMode.Code:
                    if (_assemblerBase.HasFeature(AssemblerFeatures.OptimizedAlign))
                    {
                        const int maxNopSize = 9;
                        var nopData = new int[maxNopSize][];
                        nopData[0] = new[] { 0x90 };
                        nopData[1] = new[] { 0x66, 0x90 };
                        nopData[2] = new[] { 0x0F, 0x1F, 0x00 };
                        nopData[3] = new[] { 0x0F, 0x1F, 0x40, 0x00 };
                        nopData[4] = new[] { 0x0F, 0x1F, 0x44, 0x00, 0x00 };
                        nopData[5] = new[] { 0x66, 0x0F, 0x1F, 0x44, 0x00, 0x00 };
                        nopData[6] = new[] { 0x0F, 0x1F, 0x80, 0x00, 0x00, 0x00, 0x00 };
                        nopData[7] = new[] { 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        nopData[8] = new[] { 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        do
                        {
                            var n = Math.Min(i, maxNopSize);
                            var p = nopData[n - 1/**maxNopSize*/];

                            i -= n;
                            var c = 0;
                            do
                            {
                                EmitByte(p[c]);
                            } while (--n != 0);
                        } while (i != 0);
                    }
                    pattern = 0x90;
                    break;
                case AligningMode.Data:
                    pattern = 0xCC;
                    break;
                case AligningMode.Zero:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("alignMode", alignMode, "unknown alignMode");
            }
            while (i != 0)
            {
                EmitByte(pattern);
                i--;
            }
        }

        private int RelocateCode(ref Pointer dst, Pointer baseAddress = default)
        {
            if (baseAddress == default || baseAddress == Pointer.Invalid) baseAddress = dst;

            var minCodeSize = Offset; // Current offset is the minimum code size.
            var maxCodeSize = CodeSize; // Includes all possible trampolines.

            // We will copy the exact size of the generated code. Extra code for trampolines
            // is generated on-the-fly by the relocator (this code doesn't exist at the moment).
            UnsafeMemory.Copy(dst, _buffer, minCodeSize);
            Win32.NativeMethods.VirtualProtect(dst, (UIntPtr)minCodeSize, Win32.MemoryProtectionType.Execute, out Win32.MemoryProtectionType old);
            // Trampoline pointer.
            var tramp = dst + minCodeSize;

            // Relocate all recorded locations.
            //			var relocCount = _relocList.Count;
            //			const RelocData* rdList = _relocList.getData();

            foreach (var rd in _relocations)
            {
                // Make sure that the `RelocData` is correct.
                var ptr = rd.Data;

                var offset = rd.From;
                if ((int)(offset + rd.Size) > maxCodeSize) throw new ArgumentException(string.Format("(offset:{0} + rd.Size:{1}) must be less than maxCodeSize:{2}", offset, rd.Size, maxCodeSize));

                // Whether to use trampoline, can be only used if relocation type is
                // kRelocAbsToRel on 64-bit.
                var useTrampoline = false;

                switch (rd.Mode)
                {
                    case RelocationMode.AbsToAbs:
                        break;

                    case RelocationMode.RelToAbs:
                        ptr += baseAddress;
                        break;

                    case RelocationMode.AbsToRel:
                        ptr -= baseAddress + rd.From + 4;
                        break;

                    case RelocationMode.Trampoline:
                        ptr -= baseAddress + rd.From + 4;
                        if (!((long)ptr).IsInt32())
                        {
                            ptr = tramp - (baseAddress + rd.From + 4);
                            useTrampoline = true;
                        }
                        break;

                    default:
                        throw new AccessViolationException(string.Format("unknown rd.Mode({0})", rd.Mode));
                }

                switch (rd.Size)
                {
                    case 8:
                        (dst + offset).SetI64((long)ptr);
                        break;

                    case 4:
                        (dst + offset).SetI32((int)ptr);
                        break;

                    default:
                        throw new AccessViolationException(string.Format("incorrect relocation size:{0}", rd.Size));
                }

                // Handle the trampoline case.
                if (useTrampoline)
                {
                    // Bytes that replace [REX, OPCODE] bytes.
                    byte byte0 = 0xFF;
                    byte byte1 = dst.GetUI8((int)(long)(offset - 1));

                    // Call, patch to FF/2 (-> 0x15).
                    if (byte1 == 0xE8) byte1 = (byte)EncodeMod(0, 2, 5);
                    // Jmp, patch to FF/4 (-> 0x25).
                    else if (byte1 == 0xE9) byte1 = (byte)EncodeMod(0, 4, 5);

                    // Patch `jmp/call` instruction.
                    if ((int)offset < 2) throw new ArgumentException(string.Format("offset:{0} must be greater than 2", offset));
                    dst.SetUI8(byte0, (int)(offset - 2));
                    dst.SetUI8(byte1, (int)(offset - 1));

                    // Absolute address.
                    tramp.SetUI64((ulong)(long)rd.Data);

                    // Advance trampoline pointer.
                    tramp += 8;
                }
            }
            return Constants.X64 ? (int)(tramp - dst) : minCodeSize;
        }

        internal void Bind(int labelId)
        {
            var data = _assemblerBase.GetLabelData(labelId);

            var pos = Offset;
            var link = data.Links;
            LabelLink prev = null;

            while (link != null)
            {
                var offset = link.Offset;
                if (link.RelocationId != Constants.InvalidId)
                {
                    // Handle RelocData - We have to update RelocData information instead of
                    // patching the displacement in LabelData.
                    _relocations[link.RelocationId].Data += pos;
                }
                else
                {
                    // Not using relocId, this means that we are overwriting a real
                    // displacement in the binary stream.
                    var patchedValue = pos - offset + link.Displacement;

                    // Size of the value we are going to patch. Only BYTE/DWORD is allowed.
                    var size = GetByteAt(offset);
                    if (size != 1 && size != 4) throw new ArgumentException(string.Format("size:{0} can only be 1 or 4", size));

                    if (size == 4) SetIntAt(offset, patchedValue);
                    else
                    {
                        if (size != 1) throw new ArgumentException();
                        if (patchedValue.IsInt8()) SetByteAt(offset, (byte)(patchedValue & 0xFF));
                        else throw new ArgumentException("Illegal displacement");
                    }
                }

                prev = link.Previous;
                link = prev;
            }

            // Chain unused links.
            link = data.Links;
            if (link != null)
            {
                if (prev == null) prev = link;

                prev.Previous = _unusedLinks;
                _unusedLinks = link;
            }

            data.Offset = pos;
            data.Links = null;
        }

        private LabelLink CreateLabelLink()
        {
            var link = _unusedLinks;

            if (link != null) _unusedLinks = link.Previous;
            else link = new LabelLink();

            link.Previous = null;
            link.Offset = 0;
            link.Displacement = 0;
            link.RelocationId = -1;

            return link;
        }

        internal void Embed(Pointer data, int size)
        {
            if (RemainingSpace < size) Grow(size);
            var cursor = _cursor;
            UnsafeMemory.Copy(cursor, data, size);
            if (_cursor + size < _buffer || _cursor + size > _end) throw new ArgumentOutOfRangeException();
            _cursor = _cursor + size;
        }

        private byte GetByteAt(int pos)
        {
            if (pos + sizeof(byte) > Capacity) throw new ArgumentOutOfRangeException();
            return (_buffer + pos).GetUI8();
        }

        private void SetByteAt(int pos, byte value)
        {
            if (pos + sizeof(byte) > Capacity) throw new ArgumentOutOfRangeException();
            (_buffer + pos).SetUI8(value);
        }

        private void SetIntAt(int pos, int value)
        {
            if (pos + sizeof(int) > Capacity) throw new ArgumentOutOfRangeException();
            (_buffer + pos).SetI32(value);
        }

        protected void EmitByte(int v)
        {
            _cursor.SetUI8((byte)v);
            _cursor += 1;
        }

        internal Pointer Make()
        {
            if (CodeSize == 0) return Pointer.Invalid;
            var p = UnsafeMemory.Allocate(CodeSize, true);

            // Relocate the code and release the unused memory back to `VMemMgr`.
            RelocateCode(ref p);
            return p;
        }

        internal Pointer Make(out int codeSize)
        {
            codeSize = CodeSize;
            if (CodeSize == 0) return Pointer.Invalid;
            var p = UnsafeMemory.Allocate(CodeSize, true);

            // Relocate the code and release the unused memory back to `VMemMgr`.
            var relocSize = RelocateCode(ref p);
            if (relocSize < CodeSize)
            {
                // _memoryManager.Shrink(p, relocSize);
                codeSize = relocSize;
            }
            return p;
        }

        protected void Grow(int size)
        {
            var capacity = Capacity;
            var after = Offset + size;
            // Grow is called when allocation is needed, so it shouldn't happen, but on
            // the other hand it is simple to catch and it's not an error.
            if (after <= capacity) return;

            if (capacity < MemAllocOverhead) capacity = MemAllocOverhead;
            else capacity += MemAllocOverhead;

            do
            {
                var oldCapacity = capacity;

                if (capacity < MemAllocGrowMax) capacity *= 2;
                else capacity += MemAllocGrowMax;

                // Overflow.
                if (oldCapacity > capacity) throw new OutOfMemoryException(string.Format("oldCapacity:{0} must be less than or equal to capacity capacity:{1}", oldCapacity, capacity));
            } while (capacity - MemAllocOverhead < after);

            capacity -= MemAllocOverhead;
            Reserve(capacity);
        }

        private void Reserve(int n)
        {
            if (n <= Capacity) return;

            var newBuffer = _buffer == Pointer.Invalid ? UnsafeMemory.Allocate(n) : UnsafeMemory.Reallocate(_buffer, n);

            if (newBuffer == Pointer.Invalid) throw new OutOfMemoryException("Invalid buffer");
            var offset = Offset;
            _buffer = newBuffer;
            _end = _buffer + n;
            _cursor = newBuffer + offset;
        }

        internal void Emit(InstInfo code, InstructionOptions options, params Operand[] ops)
        {
            Operand op0 = ops == null || ops.Length < 1 || ops[0] == null ? Operand.Invalid : ops[0];
            Operand op1 = ops == null || ops.Length < 2 || ops[1] == null ? Operand.Invalid : ops[1];
            Operand op2 = ops == null || ops.Length < 3 || ops[2] == null ? Operand.Invalid : ops[2];
            Operand op3 = ops == null || ops.Length < 4 || ops[3] == null ? Operand.Invalid : ops[3];

            var instructionOptions = options;

            if (RemainingSpace < 16) Grow(16);

            long primaryOpCode = code.PrimaryOpCode;
            var extendedInfo = code.ExtendedInfo;

            if (Constants.X64)
            {
                // Check if one or more register operand is one of BPL, SPL, SIL, DIL and
                // force a REX prefix to be emitted in such case.
                if (op0.IsRegister())
                {
                    var reg = op0.As<Register>();
                    if (reg.IsGpb())
                    {
                        if (reg.IsGpbLo()) instructionOptions |= reg.Index >= 4 ? InstructionOptions.Rex : 0;
                        else
                        {
                            instructionOptions |= InstructionOptions.NoRex;
                            op0 = _patchedHiRegs[reg.Index];
                        }
                    }
                }
                if (op1.IsRegister())
                {
                    var reg = op1.As<Register>();
                    if (reg.IsGpb())
                    {
                        if (reg.IsGpbLo()) instructionOptions |= reg.Index >= 4 ? InstructionOptions.Rex : 0;
                        else
                        {
                            instructionOptions |= InstructionOptions.NoRex;
                            op1 = _patchedHiRegs[reg.Index];
                        }
                    }
                }
            }
            else
            {
                // Check if one or more register operand is one of AH, BH, CH, or DH and
                // patch them to ensure that the binary code with correct byte-index (4-7)
                // is generated.
                if (op0.IsRegisterType(RegisterType.GpbHi)) op0 = _patchedHiRegs[OpReg(op0)];
                if (op1.IsRegisterType(RegisterType.GpbHi)) op1 = _patchedHiRegs[OpReg(op1)];
            }

            _eh.Init(primaryOpCode, extendedInfo.SecondaryOpCode, instructionOptions, op0, op1, op2, op3);

            if (instructionOptions.IsSet(InstructionOptions.Lock))
            {
                if (!extendedInfo.IsLockable()) throw new ArgumentException("ExtendedInfo not lockable");
                EmitByte(0xF0);
            }

            var encoding = extendedInfo.Encoding;
            var isContinue = true;
            while (isContinue)
            {
                isContinue = false;
                switch (encoding)
                {
                    case InstructionEncoding.None:
                        break;
                    case InstructionEncoding.X86Op:
                        EmitX86();
                        break;
                    case InstructionEncoding.X86Op_66H:
                        Add66Hp(1);
                        encoding = InstructionEncoding.X86Op;
                        isContinue = true;
                        break;
                    case InstructionEncoding.X86Rm:
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);
                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Rm_B:
                        _eh.OpCode += (_eh.Operand0.Size != 1).AsUInt();
                        encoding = InstructionEncoding.X86Rm;
                        isContinue = true;
                        break;
                    case InstructionEncoding.X86RmReg:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }
                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86RegRm:
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            if (op0.Size == 1 || op0.Size != op1.Size) throw new ArgumentException(string.Format("IllegalInstruction: Operands are Register & Register, but size is 1 or both are different sizes(op0 size:{0}, op1 size:{1})", op0.Size, op1.Size));

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            if (op0.Size == 1) throw new ArgumentException("IllegalInstruction: Operands are Register & Memory, but op0 size is 1)");

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86M:
                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Arith:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.OpCode += (op0.Size != 1).AsUInt() + 2;
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.OpCode += (op0.Size != 1).AsUInt() + 2;
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The remaining instructions use 0x80 opcode.
                        _eh.OpCode = 0x80;

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = _eh.ImmediateValue.IsInt8() ? 1 : Math.Min(op0.Size, 4);
                            _eh.ModRmRegister = OpReg(op0);

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            // Alternate Form - AL, AX, EAX, RAX.
                            if (_eh.ModRmRegister == 0 && (op0.Size == 1 || _eh.ImmediateLength != 1))
                            {
                                _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                                _eh.OpCode |= (_eh.Operand << 3) | (0x04 + (op0.Size != 1).AsUInt());
                                _eh.ImmediateLength = Math.Min(op0.Size, 4);
                                EmitX86();
                                break;
                            }

                            _eh.OpCode += (uint)(op0.Size != 1 ? (_eh.ImmediateLength != 1 ? 1 : 3) : 0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            var memSize = op0.Size;

                            if (memSize == 0) throw new ArgumentException("IllegalInstruction: Operands are Memory & Immediate, but op0 size is 0");

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = _eh.ImmediateValue.IsInt8() ? 1 : Math.Min(memSize, 4);

                            _eh.OpCode += (uint)(memSize != 1 ? (_eh.ImmediateLength != 1 ? 1 : 3) : 0);
                            Add66HpBySize(memSize);
                            AddRexWBySize(memSize);

                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86BSwap:
                        if (OperandsAre(OperandType.Register))
                        {
                            if (op0.Size < 4) throw new ArgumentException(string.Format("IllegalInstruction: Encoding is X86BSwap and Operands is Register, but op0 size is less than 4", op0.Size));

                            _eh.Operand = (uint)OpReg(op0);
                            AddRexWBySize(op0.Size);
                            EmitX86OpWithOpReg();
                        }
                        break;
                    case InstructionEncoding.X86BTest:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The remaining instructions use the secondary opcode/r.
                        _eh.ImmediateValue = op1.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        _eh.OpCode = extendedInfo.SecondaryOpCode;
                        _eh.Operand = ExtractO(_eh.OpCode);

                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            if (op0.Size == 0) throw new ArgumentException();

                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Call:
                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            EmitJmpOrCallAbs();
                            break;
                        }

                        if (OperandsAre(OperandType.Label))
                        {
                            _eh.Label = _assemblerBase.GetLabelData(op0.As<Label>().Id);
                            if (_eh.Label.Offset != -1)
                            {
                                // Bound label.
                                const int kRel32Size = 5;
                                var offs = _eh.Label.Offset - Offset;

                                if (!(offs <= 0)) throw new ArgumentException();
                                EmitOp(_eh.OpCode);
                                EmitDWord(offs - kRel32Size);
                            }
                            else
                            {
                                // Non-bound label.
                                EmitOp(_eh.OpCode);
                                _eh.DisplacementOffset = -4;
                                _eh.DisplacementSize = 4;
                                _eh.RelocationId = -1;
                                EmitDisplacement();
                            }
                            EmitDone();
                        }
                        break;
                    case InstructionEncoding.X86Enter:
                        if (OperandsAre(OperandType.Immediate, OperandType.Immediate))
                        {
                            EmitByte(0xC8);
                            EmitWord(op1.As<Immediate>().UInt16);
                            EmitByte(op0.As<Immediate>().UInt8);
                            EmitDone();
                        }
                        break;
                    case InstructionEncoding.X86Imul:
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                            _eh.OpCode |= 0xF6 + (op0.Size != 1).AsUInt();

                            _eh.Operand = 5;
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                            _eh.OpCode |= 0xF6 + (op0.Size != 1).AsUInt();

                            _eh.Operand = 5;
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use 0x0FAF opcode.
                        _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                        _eh.OpCode |= Constants.X86.InstOpCode_MM_0F00 | 0xAF;

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            if (!(op0.Size != 1)) throw new ArgumentException();

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            if (!(op0.Size != 1)) throw new ArgumentException();

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use 0x69/0x6B opcode.
                        _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                        _eh.OpCode |= 0x6B;

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            if (!(op0.Size != 1)) throw new ArgumentException();

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            if (!_eh.ImmediateValue.IsInt8())
                            {
                                _eh.OpCode -= 2;
                                _eh.ImmediateLength = op0.Size == 2 ? 2 : 4;
                            }

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = (int)_eh.Operand;
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            if (!(op0.Size != 1)) throw new ArgumentException();

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            if (!_eh.ImmediateValue.IsInt8())
                            {
                                _eh.OpCode -= 2;
                                _eh.ImmediateLength = op0.Size == 2 ? 2 : 4;
                            }

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            if (!(op0.Size != 1)) throw new ArgumentException();

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            if (!_eh.ImmediateValue.IsInt8())
                            {
                                _eh.OpCode -= 2;
                                _eh.ImmediateLength = op0.Size == 2 ? 2 : 4;
                            }

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86IncDec:
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op0);

                            if (!Constants.X64)
                            {
                                // INC r16|r32 is not encodable in 64-bit mode.
                                if ((op0.Size == 2 || op0.Size == 4))
                                {
                                    _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                                    _eh.OpCode |= extendedInfo.SecondaryOpCode + ((uint)_eh.ModRmRegister & 0x07);
                                    EmitX86();
                                    break;
                                }
                            }

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Int:
                        if (OperandsAre(OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            var imm8 = (byte)(_eh.ImmediateValue & 0xFF);

                            if (imm8 == 0x03) EmitOp(_eh.OpCode);
                            else
                            {
                                EmitOp(_eh.OpCode + 1);
                                EmitByte(imm8);
                            }
                            EmitDone();
                        }
                        break;
                    case InstructionEncoding.X86Jcc:
                        if (OperandsAre(OperandType.Label))
                        {
                            _eh.Label = _assemblerBase.GetLabelData(op0.As<Label>().Id);

                            if (_assemblerBase.HasFeature(AssemblerFeatures.PredictedJumps))
                            {
                                if (_eh.InstructionOptions.IsSet(InstructionOptions.Taken)) EmitByte(0x3E);
                                if (_eh.InstructionOptions.IsSet(InstructionOptions.NotTaken)) EmitByte(0x2E);
                            }

                            if (_eh.Label.Offset != -1)
                            {
                                // Bound label.
                                const int kRel8Size = 2;
                                const int kRel32Size = 6;

                                var offs = _eh.Label.Offset - Offset;
                                if (!(offs <= 0)) throw new ArgumentException();

                                if (!_eh.InstructionOptions.IsSet(InstructionOptions.LongForm) && (offs - kRel8Size).IsInt8())
                                {
                                    EmitOp(_eh.OpCode);
                                    EmitByte((byte)(offs - kRel8Size));

                                    _eh.InstructionOptions |= InstructionOptions.ShortForm;
                                    EmitDone();
                                }
                                else
                                {
                                    EmitByte(0x0F);
                                    EmitOp(_eh.OpCode + 0x10);
                                    EmitDWord(offs - kRel32Size);

                                    _eh.InstructionOptions &= ~InstructionOptions.ShortForm;
                                    EmitDone();
                                }
                            }
                            else
                            {
                                // Non-bound label.
                                if (_eh.InstructionOptions.IsSet(InstructionOptions.ShortForm))
                                {
                                    EmitOp(_eh.OpCode);
                                    _eh.DisplacementOffset = -1;
                                    _eh.DisplacementSize = 1;
                                    _eh.RelocationId = -1;
                                    EmitDisplacement();
                                }
                                else
                                {
                                    EmitByte(0x0F);
                                    EmitOp(_eh.OpCode + 0x10);
                                    _eh.DisplacementOffset = -4;
                                    _eh.DisplacementSize = 4;
                                    _eh.RelocationId = -1;
                                    EmitDisplacement();
                                }
                            }
                        }
                        break;
                    case InstructionEncoding.X86Jecxz:
                        if (OperandsAre(OperandType.Register, OperandType.Label))
                        {
                            if (OpReg(op0) != 1) throw new ArgumentException();

                            if (Constants.X64) { if (op0.Size == 4) EmitByte(0x67); }
                            else if (op0.Size == 2) EmitByte(0x67);

                            EmitByte(0xE3);
                            _eh.Label = _assemblerBase.GetLabelData(op1.As<Label>().Id);

                            if (_eh.Label.Offset != -1)
                            {
                                // Bound label.
                                var offs = _eh.Label.Offset - Offset - 1;
                                if (!offs.IsInt8()) throw new ArgumentException();

                                EmitByte((byte)offs);
                                EmitDone();
                            }
                            else
                            {
                                // Non-bound label.
                                _eh.DisplacementOffset = -1;
                                _eh.DisplacementSize = 1;
                                _eh.RelocationId = -1;
                                EmitDisplacement();
                            }
                        }
                        break;
                    case InstructionEncoding.X86Jmp:
                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use the secondary opcode (0xE9).
                        _eh.OpCode = 0xE9;

                        if (OperandsAre(OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            EmitJmpOrCallAbs();
                            break;
                        }

                        if (OperandsAre(OperandType.Label))
                        {
                            _eh.Label = _assemblerBase.GetLabelData(op0.As<Label>().Id);
                            if (_eh.Label.Offset != Constants.InvalidValue)
                            {
                                // Bound label.
                                const int rel8Size = 2;
                                const int rel32Size = 5;

                                var offs = _eh.Label.Offset - Offset;

                                if (!_eh.InstructionOptions.IsSet(InstructionOptions.LongForm) && (offs - rel8Size).IsInt8())
                                {
                                    _eh.InstructionOptions |= InstructionOptions.ShortForm;

                                    EmitByte(0xEB);
                                    EmitByte((byte)(offs - rel8Size));
                                    EmitDone();
                                    break;
                                }
                                _eh.InstructionOptions &= ~InstructionOptions.ShortForm;

                                EmitByte(0xE9);
                                EmitDWord(offs - rel32Size);
                                EmitDone();
                                break;
                            }
                            // Non-bound label.
                            if (_eh.InstructionOptions.IsSet(InstructionOptions.ShortForm))
                            {
                                EmitByte(0xEB);
                                _eh.DisplacementOffset = -1;
                                _eh.DisplacementSize = 1;
                                _eh.RelocationId = -1;
                                EmitDisplacement();
                            }
                            else
                            {
                                EmitByte(0xE9);
                                _eh.DisplacementOffset = -4;
                                _eh.DisplacementSize = 4;
                                _eh.RelocationId = -1;
                                EmitDisplacement();
                            }
                        }
                        break;
                    case InstructionEncoding.X86Lea:
                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Mov:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);

                            // Asmjit uses segment registers indexed from 1 to 6, leaving zero as
                            // "no segment register used". We have to fix this (decrement the index
                            // of the register) when emitting MOV instructions which move to/from
                            // a segment register. The segment register is always `opReg`, because
                            // the MOV instruction uses RM or MR encoding.

                            // Sreg <- Reg
                            if (OpRegType(op0) == RegisterType.Seg)
                            {
                                if (!(op1.As<Register>().IsGpw() || op1.As<Register>().IsGpd() || op1.As<Register>().IsGpq())) throw new ArgumentException();

                                // `opReg` is the segment register.
                                _eh.Operand--;
                                _eh.OpCode = 0x8E;

                                Add66HpBySize(op1.Size);
                                AddRexWBySize(op1.Size);
                                EmitX86(true);
                                break;
                            }
                            // Reg <- Sreg
                            if (OpRegType(op1) == RegisterType.Seg)
                            {
                                if (!(op0.As<Register>().IsGpw() || op0.As<Register>().IsGpd() || op0.As<Register>().IsGpq())) throw new ArgumentException();

                                // `opReg` is the segment register.
                                _eh.Operand = (uint)_eh.ModRmRegister - 1;
                                _eh.ModRmRegister = OpReg(op0);
                                _eh.OpCode = 0x8C;

                                Add66HpBySize(op0.Size);
                                AddRexWBySize(op0.Size);
                                EmitX86(true);
                                break;
                            }
                            // Reg <- Reg
                            if (!(op0.As<Register>().IsGpb() || op0.As<Register>().IsGpw() || op0.As<Register>().IsGpd() || op0.As<Register>().IsGpq())) throw new ArgumentException();

                            _eh.OpCode = 0x8A + (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);

                            // Sreg <- Mem
                            if (OpRegType(op0) == RegisterType.Seg)
                            {
                                _eh.OpCode = 0x8E;
                                _eh.Operand--;
                                Add66HpBySize(op1.Size);
                                AddRexWBySize(op1.Size);
                                EmitX86(false, true);
                                break;
                            }
                            // Reg <- Mem
                            if (!(op0.As<Register>().IsGpb() || op0.As<Register>().IsGpw() || op0.As<Register>().IsGpd() || op0.As<Register>().IsGpq())) throw new ArgumentException();
                            _eh.OpCode = 0x8A + (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);
                            EmitX86(false, true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);

                            // X86Mem <- Sreg
                            if (OpRegType(op1) == RegisterType.Seg)
                            {
                                _eh.OpCode = 0x8C;
                                Add66HpBySize(op0.Size);
                                AddRexWBySize(op0.Size);
                                EmitX86(false, true);
                                break;
                            }
                            // X86Mem <- Reg
                            if (!(op1.As<Register>().IsGpb() || op1.As<Register>().IsGpw() || op1.As<Register>().IsGpd() || op1.As<Register>().IsGpq())) throw new ArgumentException();

                            _eh.OpCode = 0x88 + (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);
                            EmitX86(false, true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            // 64-bit immediate in 64-bit mode is allowed.
                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = op0.Size;

                            _eh.Operand = 0;
                            _eh.ModRmRegister = OpReg(op0);

                            if (Constants.X64)
                            {
                                // Optimize instruction size by using 32-bit immediate if possible.
                                if (_eh.ImmediateLength == 8 && _eh.ImmediateValue.IsInt32())
                                {
                                    _eh.OpCode = 0xC7;
                                    AddRexW(1);
                                    _eh.ImmediateLength = 4;
                                    EmitX86(true);
                                    break;
                                }
                            }
                            _eh.OpCode = 0xB0 + ((op0.Size != 1).AsUInt() << 3);
                            _eh.Operand = (uint)_eh.ModRmRegister;

                            Add66HpBySize(_eh.ImmediateLength);
                            AddRexWBySize(_eh.ImmediateLength);
                            EmitX86OpWithOpReg();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            var memSize = op0.Size;

                            if (memSize == 0) throw new ArgumentException();

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = Math.Min(memSize, 4);

                            _eh.OpCode = 0xC6 + (memSize != 1).AsUInt();
                            _eh.Operand = 0;
                            Add66HpBySize(memSize);
                            AddRexWBySize(memSize);

                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86MovSxZx:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86MovSxd:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            AddRexW(1);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            AddRexW(1);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(true);
                        }
                        break;
                    case InstructionEncoding.X86MovPtr:
                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            if (OpReg(op0) != 0) throw new ArgumentException();

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = Cpu.Info.RegisterSize;
                            EmitX86();
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Immediate, OperandType.Register))
                        {
                            if (OpReg(op1) != 0) throw new ArgumentException();

                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            _eh.ImmediateLength = Cpu.Info.RegisterSize;
                            EmitX86();
                        }
                        break;
                    case InstructionEncoding.X86Push:
                        if (OperandsAre(OperandType.Register))
                        {
                            if (OpRegType(op0) == RegisterType.Seg)
                            {
                                var segment = OpReg(op0);
                                if (segment >= 7) throw new ArgumentException();

                                if (segment >= 5) EmitByte(0x0F);

                                EmitByte(_opCodePushSeg[segment]);
                                EmitDone();
                                break;
                            }
                            GroupPop_Gp();
                            break;
                        }

                        if (OperandsAre(OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            _eh.ImmediateLength = _eh.ImmediateValue.IsInt8() ? 1 : 4;

                            EmitByte((byte)(_eh.ImmediateLength == 1 ? 0x6A : 0x68));
                            EmitImm();
                            break;
                        }
                        encoding = InstructionEncoding.X86Pop;
                        isContinue = true;
                        break;
                    case InstructionEncoding.X86Pop:
                        if (OperandsAre(OperandType.Register))
                        {
                            if (OpRegType(op0) == RegisterType.Seg)
                            {
                                var segment = OpReg(op0);
                                if (segment >= 7) throw new ArgumentException();

                                if (segment >= 5) EmitByte(0x0F);

                                EmitByte(_opCodePopSeg[segment]);
                                EmitDone();
                                break;
                            }
                            GroupPop_Gp();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            if (op0.Size != 2 && op0.Size != Cpu.Info.RegisterSize) throw new ArgumentException();

                            Add66HpBySize(op0.Size);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Rep:
                        // Emit REP 0xF2 or 0xF3 prefix first.
                        EmitByte((byte)(0xF2 + _eh.Operand));
                        EmitX86();
                        break;
                    case InstructionEncoding.X86Ret:
                        if (OperandsAre(OperandType.Invalid))
                        {
                            EmitByte(0xC3);
                            EmitDone();
                            break;
                        }

                        if (OperandsAre(OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op0.As<Immediate>().Int64;
                            if (_eh.ImmediateValue == 0)
                            {
                                EmitByte(0xC3);
                                EmitDone();
                            }
                            else
                            {
                                EmitByte(0xC2);
                                _eh.ImmediateLength = 2;
                                EmitImm();
                            }
                        }
                        break;
                    case InstructionEncoding.X86Rot:
                        _eh.OpCode += (op0.Size != 1).AsUInt();
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            if (!(OpRegType(op1) == RegisterType.GpbLo && OpReg(op1) == 1)) throw new ArgumentException();
                            _eh.OpCode += 2;
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            if (!(OpRegType(op1) == RegisterType.GpbLo && OpReg(op1) == 1)) throw new ArgumentException();
                            _eh.OpCode += 2;
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op1.As<Immediate>().Int64 & 0xFF;
                            _eh.ImmediateLength = (_eh.ImmediateValue != 1).AsInt();
                            if (_eh.ImmediateLength != 0) _eh.OpCode -= 0x10;
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            if (op0.Size == 0) throw new ArgumentException();

                            _eh.ImmediateValue = op1.As<Immediate>().Int64 & 0xFF;
                            _eh.ImmediateLength = (_eh.ImmediateValue != 1).AsInt();
                            if (_eh.ImmediateLength != 0) _eh.OpCode -= 0x10;
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Set:
                        if (OperandsAre(OperandType.Register))
                        {
                            if (!(op0.Size == 1)) throw new ArgumentException();

                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            if (!(op0.Size == 1)) throw new ArgumentException();

                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Shlrd:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            if (!(op0.Size == op1.Size)) throw new ArgumentException();

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Immediate))
                        {
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use opCode + 1.
                        _eh.OpCode++;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            if (!(OpRegType(op2) == RegisterType.GpbLo && OpReg(op2) == 1)) throw new ArgumentException();
                            if (!(op0.Size == op1.Size)) throw new ArgumentException();

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Register))
                        {
                            if (!(OpRegType(op2) == RegisterType.GpbLo && OpReg(op2) == 1)) throw new ArgumentException();

                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;

                    case InstructionEncoding.X86Test:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            if (!(op0.Size == op1.Size)) throw new ArgumentException();

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instructions use the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode + (op0.Size != 1).AsUInt();
                        _eh.Operand = ExtractO(_eh.OpCode);

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = Math.Min(op0.Size, 4);

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            // Alternate Form - AL, AX, EAX, RAX.
                            if (OpReg(op0) == 0)
                            {
                                _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                                _eh.OpCode |= 0xA8 + (op0.Size != 1).AsUInt();
                                EmitX86();
                                break;
                            }

                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            if (op0.Size == 0) throw new ArgumentException();

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = Math.Min(op0.Size, 4);

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Xadd:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);

                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            // Special opcode for 'xchg ?ax, reg'.
                            if (code == Inst.Xchg && op0.Size > 1 && (_eh.Operand == 0 || _eh.ModRmRegister == 0))
                            {
                                _eh.OpCode &= Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_W;
                                _eh.OpCode |= 0x90;
                                // One of `xchg a, b` or `xchg b, a` is AX/EAX/RAX.
                                _eh.Operand = _eh.Operand + _eh.ModRmRegister;
                                EmitX86OpWithOpReg();
                                break;
                            }

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.OpCode += (op1.Size != 1).AsUInt();
                            Add66HpBySize(op1.Size);
                            AddRexWBySize(op1.Size);

                            _eh.Operand = (uint)OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.X86Xchg:
                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            Add66HpBySize(op0.Size);
                            AddRexWBySize(op0.Size);

                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        encoding = InstructionEncoding.X86Xadd;
                        isContinue = true;
                        break;
                    case InstructionEncoding.FpuOp:
                        EmitFpuOp();
                        break;
                    case InstructionEncoding.FpuArith:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = (uint)OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);

                            // We switch to the alternative opcode if the first operand is zero.
                            if (_eh.Operand == 0)
                            {
                                EmitFpArith_Reg();
                                break;
                            }
                            if (_eh.ModRmRegister == 0)
                            {
                                _eh.ModRmRegister = (int)_eh.Operand;
                                _eh.OpCode = 0xDC00 + ((_eh.OpCode >> 0) & 0xFF) + (uint)_eh.ModRmRegister;
                                EmitFpuOp();
                                break;
                            }
                            throw new ArgumentException();
                            //break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            // 0xD8/0xDC, depends on the size of the memory operand; opReg has been
                            // set already.
                            EmitFpArith_Mem();
                        }
                        break;
                    case InstructionEncoding.FpuCom:
                        if (OperandsAre(OperandType.Invalid))
                        {
                            _eh.ModRmRegister = 1;
                            EmitFpArith_Reg();
                            break;
                        }

                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op0);
                            EmitFpArith_Reg();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            EmitFpArith_Mem();
                        }
                        break;
                    case InstructionEncoding.FpuFldFst:
                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);

                            if (op0.Size == 4 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem4))
                            {
                                EmitX86(false, true);
                                break;
                            }

                            if (op0.Size == 8 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem8))
                            {
                                _eh.OpCode += 4;
                                EmitX86(false, true);
                                break;
                            }

                            if (op0.Size == 10 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem10))
                            {
                                _eh.OpCode = extendedInfo.SecondaryOpCode;
                                _eh.Operand = ExtractO(_eh.OpCode);
                                EmitX86(false, true);
                                break;
                            }
                        }

                        if (OperandsAre(OperandType.Register))
                        {
                            if (code == Inst.Fld)
                            {
                                _eh.OpCode = 0xD9C0 + (uint)OpReg(op0);
                                EmitFpuOp();
                                break;
                            }

                            if (code == Inst.Fst)
                            {
                                _eh.OpCode = 0xDDD0 + (uint)OpReg(op0);
                                EmitFpuOp();
                                break;
                            }

                            if (code == Inst.Fstp)
                            {
                                _eh.OpCode = 0xDDD8 + (uint)OpReg(op0);
                                EmitFpuOp();
                            }
                        }
                        break;
                    case InstructionEncoding.FpuM:
                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);

                            if (op0.Size == 2 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem2))
                            {
                                _eh.OpCode += 4;
                                EmitX86(false, true);
                                break;
                            }

                            if (op0.Size == 4 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem4))
                            {
                                EmitX86(false, true);
                                break;
                            }

                            if (op0.Size == 8 && extendedInfo.InstructionFlags.IsSet(Constants.X86.InstFlagMem8))
                            {
                                _eh.OpCode = extendedInfo.SecondaryOpCode;
                                _eh.Operand = ExtractO(_eh.OpCode);
                                EmitX86(false, true);
                            }
                        }
                        break;
                    case InstructionEncoding.FpuR:
                        if (OperandsAre(OperandType.Register))
                        {
                            _eh.OpCode += (uint)OpReg(op0);
                            EmitFpuOp();
                        }
                        break;
                    case InstructionEncoding.FpuRDef:
                        if (OperandsAre(OperandType.Invalid))
                        {
                            _eh.OpCode += 1;
                            EmitFpuOp();
                            break;
                        }
                        encoding = InstructionEncoding.FpuR;
                        isContinue = true;
                        break;
                    case InstructionEncoding.FpuStsw:
                        if (OperandsAre(OperandType.Register))
                        {
                            if (OpReg(op0) != 0) throw new ArgumentException();

                            _eh.OpCode = extendedInfo.SecondaryOpCode;
                            EmitFpuOp();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtRm:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtRm_P:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            Add66Hp((byte)((OpRegType(op0) == RegisterType.Xmm).AsInt() | (OpRegType(op1) == RegisterType.Xmm).AsInt()));

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            Add66Hp((OpRegType(op0) == RegisterType.Xmm).AsByte());

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtRm_Q:
                        AddRexW((OpRegType(op0) == RegisterType.Gpq || OpRegType(op1) == RegisterType.Gpq || (op1.IsMemory() && op1.Size == 8)).AsByte());
                        encoding = InstructionEncoding.ExtRm;
                        isContinue = true;
                        break;
                    case InstructionEncoding.ExtRm_PQ:
                        Add66Hp((OpRegType(op0) == RegisterType.Xmm || OpRegType(op1) == RegisterType.Xmm).AsByte());
                        encoding = InstructionEncoding.ExtRm_Q;
                        isContinue = true;
                        break;
                    case InstructionEncoding.ExtRmRi:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;
                        _eh.Operand = ExtractO(_eh.OpCode);

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                        }
                        break;
                    case InstructionEncoding.ExtRmRi_P:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            Add66Hp((byte)((OpRegType(op0) == RegisterType.Xmm).AsInt() | (OpRegType(op1) == RegisterType.Xmm).AsInt()));

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            Add66Hp((OpRegType(op0) == RegisterType.Xmm).AsByte());

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;
                        _eh.Operand = ExtractO(_eh.OpCode);

                        if (OperandsAre(OperandType.Register, OperandType.Immediate))
                        {
                            Add66Hp((OpRegType(op0) == RegisterType.Xmm).AsByte());

                            _eh.ImmediateValue = op1.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                        }
                        break;
                    case InstructionEncoding.ExtRmi:
                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtRmi_P:
                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            Add66Hp((byte)((OpRegType(op0) == RegisterType.Xmm).AsInt() | (OpRegType(op1) == RegisterType.Xmm).AsInt()));

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            Add66Hp((OpRegType(op0) == RegisterType.Xmm).AsByte());

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtCrc:
                        Add66HpBySize(op0.Size);
                        AddRexWBySize(op0.Size);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            if (!(OpRegType(op0) > RegisterType.Gpd && OpRegType(op0) < RegisterType.Gpq)) throw new ArgumentException();

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            if (!(OpRegType(op0) > RegisterType.Gpd && OpRegType(op0) < RegisterType.Gpq)) throw new ArgumentException();

                            _eh.OpCode += (op0.Size != 1).AsUInt();
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtExtrW:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            Add66Hp((OpRegType(op1) == RegisterType.Xmm).AsByte());

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Immediate))
                        {
                            // Secondary opcode of 'pextrw' instruction (SSE4.1).
                            _eh.OpCode = extendedInfo.SecondaryOpCode;
                            Add66Hp((OpRegType(op1) == RegisterType.Xmm).AsByte());

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtExtract:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            Add66Hp((OpRegType(op1) == RegisterType.Xmm).AsByte());

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Immediate))
                        {
                            Add66Hp((OpRegType(op1) == RegisterType.Xmm).AsByte());

                            _eh.ImmediateValue = op2.As<Immediate>().Int64;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtFence:

                        if (Constants.X64)
                        {
                            if (_eh.OpCode.IsSet(Constants.X86.InstOpCode_W_Mask)) EmitByte(Constants.X86.ByteRex | Constants.X86.ByteRexW);
                        }

                        EmitByte(0x0F);
                        EmitOp(_eh.OpCode);
                        EmitByte((byte)(0xC0 | ((int)_eh.Operand << 3)));
                        EmitDone();
                        break;
                    case InstructionEncoding.ExtMov:
                    case InstructionEncoding.ExtMovNoRexW:
                        if (!(extendedInfo.OperandFlags[0] != 0)) throw new ArgumentException();
                        if (!(extendedInfo.OperandFlags[1] != 0)) throw new ArgumentException();

                        // Check parameters Gpd|Gpq|Mm|Xmm <- Gpd|Gpq|Mm|Xmm|X86Mem|Imm.
                        Debug.Assert(!(
                            (op0.IsMemory() && (extendedInfo.OperandFlags[0] & Constants.X86.InstOpMem) == 0) ||
                            (op0.IsRegisterType(RegisterType.Mm) && (extendedInfo.OperandFlags[0] & Constants.X86.InstOpMm) == 0) ||
                            (op0.IsRegisterType(RegisterType.Xmm) && (extendedInfo.OperandFlags[0] & Constants.X86.InstOpXmm) == 0) ||
                            (op0.IsRegisterType(RegisterType.Gpd) && (extendedInfo.OperandFlags[0] & Constants.X86.InstOpGd) == 0) ||
                            (op0.IsRegisterType(RegisterType.Gpq) && (extendedInfo.OperandFlags[0] & Constants.X86.InstOpGq) == 0) ||

                            (op1.IsMemory() && (extendedInfo.OperandFlags[1] & Constants.X86.InstOpMem) == 0) ||
                            (op1.IsRegisterType(RegisterType.Mm) && (extendedInfo.OperandFlags[1] & Constants.X86.InstOpMm) == 0) ||
                            (op1.IsRegisterType(RegisterType.Xmm) && (extendedInfo.OperandFlags[1] & Constants.X86.InstOpXmm) == 0) ||
                            (op1.IsRegisterType(RegisterType.Gpd) && (extendedInfo.OperandFlags[1] & Constants.X86.InstOpGd) == 0) ||
                            (op1.IsRegisterType(RegisterType.Gpq) && (extendedInfo.OperandFlags[1] & Constants.X86.InstOpGq) == 0)));

                        // Gp|Mm|Xmm <- Gp|Mm|Xmm
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            AddRexW((OpRegType(op0) == RegisterType.Gpq && (extendedInfo.Encoding != InstructionEncoding.ExtMovNoRexW)).AsByte());
                            AddRexW((OpRegType(op1) == RegisterType.Gpq && (extendedInfo.Encoding != InstructionEncoding.ExtMovNoRexW)).AsByte());

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        // Gp|Mm|Xmm <- Mem
                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            AddRexW((OpRegType(op0) == RegisterType.Gpq && (extendedInfo.Encoding != InstructionEncoding.ExtMovNoRexW)).AsByte());

                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instruction uses opCode[1].
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        // X86Mem <- Gp|Mm|Xmm
                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            AddRexW((OpRegType(op1) == RegisterType.Gpq && (extendedInfo.Encoding != InstructionEncoding.ExtMovNoRexW)).AsByte());

                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtMovBe:
                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            if (_eh.Operand0.Size == 1) throw new ArgumentException();

                            Add66HpBySize(_eh.Operand0.Size);
                            AddRexWBySize(_eh.Operand0.Size);

                            _eh.Operand = OpReg(_eh.Operand0);
                            _eh.ModRmMemory = OpMem(_eh.Operand1);
                            EmitX86(false, true);
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            if (_eh.Operand1.Size == 1) throw new ArgumentException();

                            Add66HpBySize(_eh.Operand1.Size);
                            AddRexWBySize(_eh.Operand1.Size);

                            _eh.Operand = OpReg(_eh.Operand1);
                            _eh.ModRmMemory = OpMem(_eh.Operand0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtMovD:
                        EmitMmMovD();
                        break;
                    case InstructionEncoding.ExtMovQ:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);

                            // Mm <- Mm
                            if (OpRegType(op0) == RegisterType.Mm && OpRegType(op1) == RegisterType.Mm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_00 | Constants.X86.InstOpCode_MM_0F00 | 0x6F;
                                EmitX86(true);
                                break;
                            }

                            // Xmm <- Xmm
                            if (OpRegType(op0) == RegisterType.Xmm && OpRegType(op1) == RegisterType.Xmm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_F3 | Constants.X86.InstOpCode_MM_0F00 | 0x7E;
                                EmitX86(true);
                                break;
                            }

                            // Mm <- Xmm (Movdq2q)
                            if (OpRegType(op0) == RegisterType.Mm && OpRegType(op1) == RegisterType.Xmm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_F2 | Constants.X86.InstOpCode_MM_0F00 | 0xD6;
                                EmitX86(true);
                                break;
                            }

                            // Xmm <- Mm (Movq2dq)
                            if (OpRegType(op0) == RegisterType.Xmm && OpRegType(op1) == RegisterType.Mm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_F3 | Constants.X86.InstOpCode_MM_0F00 | 0xD6;
                                EmitX86(true);
                                break;
                            }
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);

                            // Mm <- Mem
                            if (OpRegType(op0) == RegisterType.Mm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_00 | Constants.X86.InstOpCode_MM_0F00 | 0x6F;
                                EmitX86(false, true);
                                break;
                            }

                            // Xmm <- Mem
                            if (OpRegType(op0) == RegisterType.Xmm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_F3 | Constants.X86.InstOpCode_MM_0F00 | 0x7E;
                                EmitX86(false, true);
                                break;
                            }
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);

                            // X86Mem <- Mm
                            if (OpRegType(op1) == RegisterType.Mm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_00 | Constants.X86.InstOpCode_MM_0F00 | 0x7F;
                                EmitX86(false, true);
                                break;
                            }

                            // X86Mem <- Xmm
                            if (OpRegType(op1) == RegisterType.Xmm)
                            {
                                _eh.OpCode = Constants.X86.InstOpCode_PP_66 | Constants.X86.InstOpCode_MM_0F00 | 0xD6;
                                EmitX86(false, true);
                                break;
                            }
                        }

                        if (Constants.X64)
                        {
                            // Movq in other case is simply a MOVD instruction promoted to 64-bit.
                            _eh.OpCode |= Constants.X86.InstOpCode_W;
                            EmitMmMovD();
                        }
                        break;
                    case InstructionEncoding.ExtPrefetch:
                        if (OperandsAre(OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = op1.As<Immediate>().UInt32 & 0x3;
                            _eh.ModRmMemory = OpMem(op0);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.ExtExtrq:
                        _eh.Operand = OpReg(op0);
                        _eh.ModRmRegister = OpReg(op1);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            EmitX86(true);
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Register, OperandType.Immediate, OperandType.Immediate))
                        {
                            _eh.ImmediateValue = op1.As<Immediate>().UInt32 + (op2.As<Immediate>().UInt32 << 8);
                            _eh.ImmediateLength = 2;

                            _eh.ModRmRegister = (int)ExtractO(_eh.OpCode);
                            EmitX86(true);
                        }
                        break;
                    case InstructionEncoding.ExtInsertq:
                        _eh.Operand = OpReg(op0);
                        _eh.ModRmRegister = OpReg(op1);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            EmitX86(true);
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate) && op3.OperandType == OperandType.Immediate)
                        {
                            _eh.ImmediateValue = op2.As<Immediate>().UInt32 + (op3.As<Immediate>().UInt32 << 8);
                            _eh.ImmediateLength = 2;
                            EmitX86(true);
                        }
                        break;
                    case InstructionEncoding.Amd3dNow:
                        // Every 3dNow instruction starts with 0x0F0F and the actual opcode is
                        // stored as 8-bit immediate.
                        _eh.ImmediateValue = _eh.OpCode & 0xFF;
                        _eh.ImmediateLength = 1;

                        _eh.OpCode = Constants.X86.InstOpCode_MM_0F00 | 0x0F;
                        _eh.Operand = OpReg(op0);

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.ModRmRegister = OpReg(op1);
                            EmitX86(true);
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op1);
                            EmitX86(false, true);
                        }
                        break;
                    case InstructionEncoding.AvxOp:
                        EmitAvxOp();
                        break;
                    case InstructionEncoding.AvxM:
                        if (OperandsAre(OperandType.Memory))
                        {
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxMr:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxMr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxMr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxMri:
                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxMri_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxMri;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRm:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRm_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRm;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRmi:
                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvm:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvm_P:
                        AddVexL((OpRegType(op0) == RegisterType.Ymm || OpRegType(op1) == RegisterType.Ymm).AsByte());
                        encoding = InstructionEncoding.AvxRvm;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvmr:
                        if (op3.OperandType != OperandType.Register) throw new ArgumentException();

                        _eh.ImmediateValue = (long)OpReg(op3) << 4;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvmr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvmi:
                        if (op3.OperandType != OperandType.Immediate) throw new ArgumentException();

                        _eh.ImmediateValue = op3.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRmv:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRmvi:
                        if (op3.OperandType != OperandType.Immediate) throw new ArgumentException();

                        _eh.ImmediateValue = op3.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRmMr:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        AvxRmMr_AfterRegRegCheck();
                        break;
                    case InstructionEncoding.AvxRmMr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRmMr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvmRmi:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                            break;
                        }

                        // The following instructions use the secondary opcode.
                        _eh.OpCode &= Constants.X86.InstOpCode_L_Mask;
                        _eh.OpCode |= extendedInfo.SecondaryOpCode;

                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmRmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvmRmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvmMr:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                            break;
                        }

                        // The following instructions use the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmRegister = OpReg(op0);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmMvr:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode &= Constants.X86.InstOpCode_L_Mask;
                        _eh.OpCode |= extendedInfo.SecondaryOpCode;

                        if (OperandsAre(OperandType.Memory, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op2), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmMvr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvmMvr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvmVmi:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitAvxMAndSib();
                            break;
                        }

                        // The following instruction uses the secondary opcode.
                        _eh.OpCode &= Constants.X86.InstOpCode_L_Mask;
                        _eh.OpCode |= extendedInfo.SecondaryOpCode;
                        _eh.Operand = ExtractO(_eh.OpCode);

                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvmVmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvmVmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxVm:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxVmi:
                        _eh.ImmediateValue = op3.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = RegAndVvvv(_eh.Operand, OpReg(op0));
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxVmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxVmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxRvrmRvmr:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);

                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsMemory())
                        {
                            _eh.ImmediateValue = OpReg(op2) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op3);

                            AddVexW(1);
                            EmitAvxMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);

                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxRvrmRvmr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.AvxRvrmRvmr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.AvxMovDQ:
                        //FIXME isContinue?
                        break;
                    case InstructionEncoding.AvxMovSsSd:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitAvxMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Memory, OperandType.Register))
                        {
                            _eh.OpCode = extendedInfo.SecondaryOpCode;
                            _eh.Operand = OpReg(op1);
                            _eh.ModRmMemory = OpMem(op0);
                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.AvxGather:
                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);

                            var vSib = _eh.ModRmMemory.VSib;
                            if (vSib == Constants.X86.MemVSibGpz) throw new ArgumentException();

                            AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                            EmitAvxV();
                        }
                        break;
                    case InstructionEncoding.AvxGatherEx:
                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);

                            var vSib = _eh.ModRmMemory.VSib;
                            if (vSib == Constants.X86.MemVSibGpz) throw new ArgumentException();

                            AddVexL((vSib == Constants.X86.MemVSibYmm).AsByte());
                            EmitAvxV();
                        }
                        break;
                    case InstructionEncoding.Fma4:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);

                            EmitAvxR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsMemory())
                        {
                            _eh.ImmediateValue = OpReg(op2) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op3);

                            AddVexW(1);
                            EmitAvxMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);

                            EmitAvxMAndSib();
                        }
                        break;
                    case InstructionEncoding.Fma4_P:
                        // It's fine to just check the first operand, second is just for sanity.
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.Fma4;
                        isContinue = true;
                        break;
                    case InstructionEncoding.XopRm:
                        if (OperandsAre(OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitXopMAndSib();
                        }
                        break;
                    case InstructionEncoding.XopRm_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.XopRm;
                        isContinue = true;
                        break;
                    case InstructionEncoding.XopRvmRmv:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmRegister = OpReg(op1);

                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);

                            EmitXopMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);

                            AddVexW(1);
                            EmitXopMAndSib();
                        }

                        break;
                    case InstructionEncoding.XopRvmRmi:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmRegister = OpReg(op1);
                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op2));
                            _eh.ModRmMemory = OpMem(op1);

                            EmitXopMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);

                            AddVexW(1);
                            EmitXopR();
                            break;
                        }

                        // The following instructions use the secondary opcode.
                        _eh.OpCode = extendedInfo.SecondaryOpCode;

                        _eh.ImmediateValue = op2.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmRegister = OpReg(op1);
                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Memory, OperandType.Immediate))
                        {
                            _eh.Operand = OpReg(op0);
                            _eh.ModRmMemory = OpMem(op1);
                            EmitXopMAndSib();
                        }
                        break;
                    case InstructionEncoding.XopRvmr:
                        if (op3.OperandType != OperandType.Register) throw new ArgumentException();

                        _eh.ImmediateValue = OpReg(op3) << 4;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitXopMAndSib();
                        }
                        break;
                    case InstructionEncoding.XopRvmr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.XopRvmr;
                        isContinue = true;
                        break;
                    case InstructionEncoding.XopRvmi:
                        if (op3.OperandType != OperandType.Immediate) throw new ArgumentException();

                        _eh.ImmediateValue = op3.As<Immediate>().Int64;
                        _eh.ImmediateLength = 1;

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);
                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory))
                        {
                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);
                            EmitXopMAndSib();
                        }
                        break;
                    case InstructionEncoding.XopRvmi_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.XopRvmi;
                        isContinue = true;
                        break;
                    case InstructionEncoding.XopRvrmRvmr:
                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmRegister = OpReg(op2);

                            EmitXopR();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Register) && op3.IsMemory())
                        {
                            _eh.ImmediateValue = OpReg(op2) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op3);

                            AddVexW(1);
                            EmitXopMAndSib();
                            break;
                        }

                        if (OperandsAre(OperandType.Register, OperandType.Register, OperandType.Memory) && op3.IsRegister())
                        {
                            _eh.ImmediateValue = OpReg(op3) << 4;
                            _eh.ImmediateLength = 1;

                            _eh.Operand = RegAndVvvv(OpReg(op0), OpReg(op1));
                            _eh.ModRmMemory = OpMem(op2);

                            EmitXopMAndSib();
                        }
                        break;
                    case InstructionEncoding.XopRvrmRvmr_P:
                        AddVexL((byte)((OpRegType(op0) == RegisterType.Ymm).AsInt() | (OpRegType(op1) == RegisterType.Ymm).AsInt()));
                        encoding = InstructionEncoding.XopRvrmRvmr;
                        isContinue = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("unknown encoding:{0}", encoding));
                }
            }
        }

        private bool OperandsAre(OperandType t0, OperandType t1 = OperandType.Invalid, OperandType t2 = OperandType.Invalid)
        {
            var is0 = _eh.Operand0 != null && _eh.Operand0.OperandType == t0;
            var is1 = _eh.Operand1 != null && _eh.Operand1.OperandType == t1;
            var is2 = _eh.Operand2 != null && _eh.Operand2.OperandType == t2;
            return is0 && is1 && is2;
            //return (_eh.Operand0 == null || t0 == OperandType.Invalid || _eh.Operand0.OperandType == t0) && (_eh.Operand1 == null || t1 == OperandType.Invalid || _eh.Operand1.OperandType == t1) && (_eh.Operand2 == null || t2 == OperandType.Invalid || _eh.Operand2.OperandType == t2);
        }

        private void EmitByte(byte value)
        {
            _cursor.SetUI8(value);
            _cursor += 1;
        }

        private void EmitWord(ushort value)
        {
            _cursor.SetUI16(value);
            _cursor += 2;
        }

        private void EmitDWord(int value)
        {
            _cursor.SetUI32((uint)value);
            _cursor += 4;
        }

        private void EmitQWord(long value)
        {
            _cursor.SetUI64((ulong)value);
            _cursor += 8;
        }

        private void EmitOp(long value) => EmitByte((byte)(value & 0xFF));

        private void EmitPp(long value)
        {
            var ppIndex = (value >> (int)Constants.X86.InstOpCode_PP_Shift) & (Constants.X86.InstOpCode_PP_Mask >> (int)Constants.X86.InstOpCode_PP_Shift);
            var ppCode = _opCodePp[ppIndex];
            if (ppIndex == 0) return;
            _cursor.SetUI8(ppCode);
            _cursor += 1;
        }

        private void EmitMm(long value)
        {
            var mmIndex = (value >> (int)Constants.X86.InstOpCode_MM_Shift) & (Constants.X86.InstOpCode_MM_Mask >> (int)Constants.X86.InstOpCode_MM_Shift);
            var mmCode = _opCodeMm[mmIndex];
            if (mmIndex == 0) return;
            _cursor.SetUI8(mmCode.Data[0]);
            _cursor.SetUI8(mmCode.Data[1], 1);
            _cursor += mmCode.Length;
        }

        private void EmitX86(bool is86R = false, bool is86M = false) //default is X86Op
        {
            if (is86M) //X86M
            {
                if (_eh.ModRmMemory == null) throw new ArgumentException();

                _eh.Base = _eh.ModRmMemory.Base;
                _eh.Index = _eh.ModRmMemory.Index;

                // Size override prefix.
                if (_eh.ModRmMemory.HasBaseOrIndex() && _eh.ModRmMemory.MemoryType != MemoryType.Label)
                {
                    if (Constants.X64) { if (_eh.ModRmMemory.HasGdpBase) EmitByte(0x67); }
                    else if (!_eh.ModRmMemory.HasGdpBase) EmitByte(0x67);
                }

                // Segment override prefix.
                if (_eh.ModRmMemory.HasSegment) EmitByte(_segmentPrefix[_eh.ModRmMemory.Segment]);
            }

            // Mandatory instruction prefix.
            EmitPp(_eh.OpCode);

            if (Constants.X64)
            {
                // Rex prefix (64-bit only).
                var rex = RexFromOpCodeAndOptions(_eh.OpCode, _eh.InstructionOptions);

                if (is86R || is86M) rex += (_eh.Operand & 0x08) >> 1; // Rex.R (0x04).
                if (is86R) rex += _eh.ModRmRegister >> 3; // Rex.B (0x01).
                else if (is86M)
                {
                    rex += ((ulong)_eh.Index - 8 < 8).AsUInt() << 1; // Rex.X (0x02).
                    rex += ((ulong)_eh.Base - 8 < 8).AsUInt(); // Rex.B (0x01).
                }

                if ((rex & ~Constants.X86._InstOptionNoRex) != 0)
                {
                    rex |= Constants.X86.ByteRex;
                    if (is86R || is86M) _eh.Operand &= 0x07;
                    if (is86R) _eh.ModRmRegister &= 0x07;
                    EmitByte((byte)rex);

                    if (rex >= Constants.X86._InstOptionNoRex) throw new ArgumentException();
                }
                if (is86M) _eh.Base &= 0x07;
            }
            // Instruction opcodes.
            EmitMm(_eh.OpCode);
            EmitOp(_eh.OpCode);

            if (is86R) EmitByte((byte)EncodeMod(3, _eh.Operand, _eh.ModRmRegister)); // ModR.
            if (is86M) EmitSib();
            else
            {
                if (_eh.ImmediateLength != 0) EmitImm();
                else EmitDone();
            }
        }

        private void EmitAvxM()
        {
            if (_eh.ModRmMemory == null) throw new ArgumentException("EmitContextData ModRmMemory is null");

            if (_eh.ModRmMemory.HasSegment) EmitByte(_segmentPrefix[_eh.ModRmMemory.Segment]);

            _eh.Base = _eh.ModRmMemory.Base;
            _eh.Index = _eh.ModRmMemory.Index;

            var vexXvvvvLpp = (_eh.OpCode >> (int)(Constants.X86.InstOpCode_W_Shift - 7)) & 0x80;
            vexXvvvvLpp += (_eh.OpCode >> (int)(Constants.X86.InstOpCode_L_Shift - 2)) & 0x04;
            vexXvvvvLpp += (_eh.OpCode >> (int)Constants.X86.InstOpCode_PP_Shift) & 0x03;
            vexXvvvvLpp += _eh.Operand >> (int)(Constants.X86.VexVVVVShift - 3);

            var vexRxbmmmmm = (_eh.OpCode >> (int)Constants.X86.InstOpCode_MM_Shift) & 0x0F;
            vexRxbmmmmm |= ((ulong)_eh.Base - 8 < 8).AsUInt() << 5;
            vexRxbmmmmm |= ((ulong)_eh.Index - 8 < 8).AsUInt() << 6;

            if ((vexRxbmmmmm != 0x01) || (vexXvvvvLpp >= 0x80) || _eh.InstructionOptions.IsSet(InstructionOptions.Vex3))
            {
                vexRxbmmmmm |= (_eh.Operand << 4) & 0x80;
                vexRxbmmmmm ^= 0xE0;
                vexXvvvvLpp ^= 0x78;

                EmitByte(Constants.X86.ByteVex3);
                EmitByte((byte)vexRxbmmmmm);
                EmitByte((byte)vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }
            else
            {
                vexXvvvvLpp |= (_eh.Operand << 4) & 0x80;
                vexXvvvvLpp ^= 0xF8;

                EmitByte(Constants.X86.ByteVex2);
                EmitByte((byte)vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }
            _eh.Base &= 0x07;
            _eh.Operand &= 0x07;
        }

        private void EmitAvxOp()
        {
            var vexXvvvvLpp = (_eh.OpCode >> (int)(Constants.X86.InstOpCode_L_Shift - 2)) & 0x04;
            vexXvvvvLpp |= _eh.OpCode >> (int)Constants.X86.InstOpCode_PP_Shift;
            vexXvvvvLpp |= 0xF8;

            // Encode 3-byte VEX prefix only if specified in options.
            if (_eh.InstructionOptions.IsSet(InstructionOptions.Vex3))
            {
                var vexRxbmmmmm = (_eh.OpCode >> (int)Constants.X86.InstOpCode_MM_Shift) | 0xE0;

                EmitByte(Constants.X86.ByteVex3);
                EmitOp(vexRxbmmmmm);
                EmitOp(vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }
            else
            {
                EmitByte(Constants.X86.ByteVex2);
                EmitOp(vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }
            EmitDone();
        }

        private void EmitAvxR()
        {
            var vexXvvvvLpp = (_eh.OpCode >> (int)(Constants.X86.InstOpCode_W_Shift - 7)) & 0x80;
            vexXvvvvLpp += (_eh.OpCode >> (int)(Constants.X86.InstOpCode_L_Shift - 2)) & 0x04;
            vexXvvvvLpp += (_eh.OpCode >> (int)Constants.X86.InstOpCode_PP_Shift) & 0x03;
            vexXvvvvLpp += _eh.Operand >> (int)(Constants.X86.VexVVVVShift - 3);

            var vexRxbmmmmm = (_eh.OpCode >> (int)Constants.X86.InstOpCode_MM_Shift) & 0x0F;
            vexRxbmmmmm |= (_eh.ModRmRegister << 2) & 0x20;

            if (vexRxbmmmmm != 0x01 || vexXvvvvLpp >= 0x80 || _eh.InstructionOptions.IsSet(InstructionOptions.Vex3))
            {
                vexRxbmmmmm |= (_eh.Operand & 0x08) << 4;
                vexRxbmmmmm ^= 0xE0;
                vexXvvvvLpp ^= 0x78;

                EmitByte(Constants.X86.ByteVex3);
                EmitOp(vexRxbmmmmm);
                EmitOp(vexXvvvvLpp);
                EmitOp(_eh.OpCode);

                _eh.ModRmRegister &= 0x07;
            }
            else
            {
                vexXvvvvLpp += (_eh.Operand & 0x08) << 4;
                vexXvvvvLpp ^= 0xF8;

                EmitByte(Constants.X86.ByteVex2);
                EmitOp(vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }

            EmitByte((byte)EncodeMod(3, _eh.Operand & 0x07, _eh.ModRmRegister));

            if (_eh.ImmediateLength == 0)
            {
                EmitDone();
                return;
            }

            EmitByte((byte)(_eh.ImmediateValue & 0xFF));
            EmitDone();
        }

        private void EmitAvxMAndSib()
        {
            EmitAvxM();
            EmitSib();
        }

        private void EmitAvxV()
        {
            EmitAvxM();

            if (_eh.Index == -1/*unchecked((uint)_eh.Index) >= unchecked((uint)-1)*/) throw new ArgumentException();

            if (Constants.X64) _eh.Index &= 0x07;

            _eh.DisplacementOffset = _eh.ModRmMemory.Displacement;
            if (_eh.ModRmMemory.IsBaseIndexType())
            {
                var shift = _eh.ModRmMemory.Shift;

                if (_eh.Base != RegisterIndex.Bp && _eh.DisplacementOffset == 0)
                {
                    // [Base + Index * Scale].
                    EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                    EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                }
                else if (_eh.DisplacementOffset.IsInt8())
                {
                    // [Base + Index * Scale + Disp8].
                    EmitByte((byte)EncodeMod(1, _eh.Operand, 4));
                    EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                    EmitByte((byte)_eh.DisplacementOffset);
                }
                else
                {
                    // [Base + Index * Scale + Disp32].
                    EmitByte((byte)EncodeMod(2, _eh.Operand, 4));
                    EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                    EmitDWord(_eh.DisplacementOffset);
                }
            }
            else
            {
                // [Index * Scale + Disp32].
                var shift = _eh.ModRmMemory.Shift;

                EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                EmitByte((byte)EncodeSib(shift, _eh.Index, 5));

                if (_eh.ModRmMemory.MemoryType == MemoryType.Label)
                {
                    if (Constants.X64) throw new InvalidDataException("IllegalAddressing: MemoryType is Label, cannot X64");

                    // Relative->Absolute [x86 mode].
                    _eh.Label = _assemblerBase.GetLabelData(_eh.ModRmMemory.Base);
                    _eh.RelocationId = _relocations.Count;

                    {
                        var rd = new RelocationData
                        {
                            Mode = RelocationMode.RelToAbs,
                            Size = 4,
                            From = _cursor - _buffer,
                            Data = (IntPtr)_eh.DisplacementOffset
                        };
                        _relocations.Add(rd);
                    }

                    if (_eh.Label.Offset != -1)
                    {
                        // Bound label.
                        _relocations[_eh.RelocationId].Data += _eh.Label.Offset;
                        EmitDWord(0);
                    }
                    else
                    {
                        // Non-bound label.
                        _eh.DisplacementOffset = -4 - _eh.ImmediateLength;
                        _eh.DisplacementSize = 4;
                        EmitDisplacement();
                        return;
                    }
                }
                else EmitDWord(_eh.DisplacementOffset); // [Disp32].
            }
            EmitDone();
        }

        private void EmitXopR()
        {
            var xopXvvvvLpp = (_eh.OpCode >> (int)(Constants.X86.InstOpCode_W_Shift - 7)) & 0x80;
            xopXvvvvLpp += (_eh.OpCode >> (int)(Constants.X86.InstOpCode_L_Shift - 2)) & 0x04;
            xopXvvvvLpp += (_eh.OpCode >> (int)Constants.X86.InstOpCode_PP_Shift) & 0x03;
            xopXvvvvLpp += _eh.Operand >> (int)(Constants.X86.VexVVVVShift - 3);

            var xopRxbmmmmm = (_eh.OpCode >> (int)Constants.X86.InstOpCode_MM_Shift) & 0x0F;
            xopRxbmmmmm |= (_eh.ModRmRegister << 2) & 0x20;

            xopRxbmmmmm |= (_eh.Operand & 0x08) << 4;
            xopRxbmmmmm ^= 0xE0;
            xopXvvvvLpp ^= 0x78;

            EmitByte(Constants.X86.ByteXop3);
            EmitOp(xopRxbmmmmm);
            EmitOp(xopXvvvvLpp);
            EmitOp(_eh.OpCode);

            _eh.ModRmRegister &= 0x07;
            EmitByte((byte)EncodeMod(3, _eh.Operand & 0x07, _eh.ModRmRegister));

            if (_eh.ImmediateLength == 0)
            {
                EmitDone();
                return;
            }

            EmitByte((byte)(_eh.ImmediateValue & 0xFF));
            EmitDone();
        }

        private void EmitXopM()
        {
            if (_eh.ModRmMemory == null) throw new ArgumentException();

            if (_eh.ModRmMemory.HasSegment) EmitByte(_segmentPrefix[_eh.ModRmMemory.Segment]);

            _eh.Base = _eh.ModRmMemory.Base;
            _eh.Index = _eh.ModRmMemory.Index;

            {
                var vexXvvvvLpp = (_eh.OpCode >> (int)(Constants.X86.InstOpCode_W_Shift - 7)) & 0x80;
                vexXvvvvLpp += (_eh.OpCode >> (int)(Constants.X86.InstOpCode_L_Shift - 2)) & 0x04;
                vexXvvvvLpp += (_eh.OpCode >> (int)Constants.X86.InstOpCode_PP_Shift) & 0x03;
                vexXvvvvLpp += _eh.Operand >> (int)(Constants.X86.VexVVVVShift - 3);

                var vexRxbmmmmm = (_eh.OpCode >> (int)Constants.X86.InstOpCode_MM_Shift) & 0x0F;
                vexRxbmmmmm += ((ulong)_eh.Base - 8 < 8).AsUInt() << 5;
                vexRxbmmmmm += ((ulong)_eh.Index - 8 < 8).AsUInt() << 6;

                vexRxbmmmmm |= (_eh.Operand << 4) & 0x80;
                vexRxbmmmmm ^= 0xE0;
                vexXvvvvLpp ^= 0x78;

                EmitByte(Constants.X86.ByteXop3);
                EmitByte((byte)vexRxbmmmmm);
                EmitByte((byte)vexXvvvvLpp);
                EmitOp(_eh.OpCode);
            }

            _eh.Base &= 0x07;
            _eh.Operand &= 0x07;
        }

        private void EmitXopMAndSib()
        {
            EmitXopM();
            EmitSib();
        }

        private void EmitSib()
        {
            _eh.DisplacementOffset = _eh.ModRmMemory.Displacement;
            if (_eh.ModRmMemory.IsBaseIndexType())
            {
                if (_eh.Index == RegisterIndex.Invalid)
                {
                    if (_eh.Base == RegisterIndex.Sp)
                    {
                        if (_eh.DisplacementOffset == 0)
                        {
                            // [Esp/Rsp/R12].
                            EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                            EmitByte((byte)EncodeSib(0, 4, 4));
                        }
                        else if (_eh.DisplacementOffset.IsInt8())
                        {
                            // [Esp/Rsp/R12 + Disp8].
                            EmitByte((byte)EncodeMod(1, _eh.Operand, 4));
                            EmitByte((byte)EncodeSib(0, 4, 4));
                            EmitByte((byte)_eh.DisplacementOffset);
                        }
                        else
                        {
                            // [Esp/Rsp/R12 + Disp32].
                            EmitByte((byte)EncodeMod(2, _eh.Operand, 4));
                            EmitByte((byte)EncodeSib(0, 4, 4));
                            EmitDWord(_eh.DisplacementOffset);
                        }
                    }
                    else if (_eh.Base != RegisterIndex.Bp && _eh.DisplacementOffset == 0)
                    {
                        // [Base].
                        EmitByte((byte)EncodeMod(0, _eh.Operand, _eh.Base));
                    }
                    else if (_eh.DisplacementOffset.IsInt8())
                    {
                        // [Base + Disp8].
                        EmitByte((byte)EncodeMod(1, _eh.Operand, _eh.Base));
                        EmitByte((byte)_eh.DisplacementOffset);
                    }
                    else
                    {
                        // [Base + Disp32].
                        EmitByte((byte)EncodeMod(2, _eh.Operand, _eh.Base));
                        EmitDWord(_eh.DisplacementOffset);
                    }
                }
                else
                {
                    var shift = _eh.ModRmMemory.Shift;

                    // Esp/Rsp/R12 register can't be used as an index.
                    _eh.Index &= 0x07;
                    if (_eh.Index == RegisterIndex.Sp) throw new ArgumentException();

                    if (_eh.Base != RegisterIndex.Bp && _eh.DisplacementOffset == 0)
                    {
                        // [Base + Index * Scale].
                        EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                        EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                    }
                    else if (_eh.DisplacementOffset.IsInt8())
                    {
                        // [Base + Index * Scale + Disp8].
                        EmitByte((byte)EncodeMod(1, _eh.Operand, 4));
                        EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                        EmitByte((byte)_eh.DisplacementOffset);
                    }
                    else
                    {
                        // [Base + Index * Scale + Disp32].
                        EmitByte((byte)EncodeMod(2, _eh.Operand, 4));
                        EmitByte((byte)EncodeSib(shift, _eh.Index, _eh.Base));
                        EmitDWord(_eh.DisplacementOffset);
                    }
                }
            }
            else
            {
                if (Constants.X64)
                {
                    switch (_eh.ModRmMemory.MemoryType)
                    {
                        case MemoryType.Absolute:
                            EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                            if (_eh.Index == -1 /*unchecked((uint)_eh.Index) >= unchecked((uint)-1)*/)
                            {
                                // [Disp32].
                                EmitByte((byte)EncodeSib(0, 4, 5));
                            }
                            else
                            {
                                // [Disp32 + Index * Scale].
                                _eh.Index &= 0x07;
                                if (_eh.Index == RegisterIndex.Sp) throw new ArgumentException();

                                var shift = _eh.ModRmMemory.Shift;
                                EmitByte((byte)EncodeSib(shift, _eh.Index, 5));
                            }
                            EmitDWord(_eh.DisplacementOffset);
                            break;
                        case MemoryType.Label:
                            // [RIP + Disp32].
                            _eh.Label = _assemblerBase.GetLabelData(_eh.ModRmMemory.Base);

                            // Indexing is invalid.
                            if (_eh.Index != -1) throw new InvalidDataException(string.Format("IllegalDisplacement: MemoryType is Label and Index:{0} is invalid", _eh.Index));

                            EmitByte((byte)EncodeMod(0, _eh.Operand, 5));
                            _eh.DisplacementOffset -= 4 + _eh.ImmediateLength;

                            if (_eh.Label.Offset != -1)
                            {
                                // Bound label.
                                _eh.DisplacementOffset += _eh.Label.Offset - Offset;
                                EmitDWord(_eh.DisplacementOffset);
                            }
                            else
                            {
                                // Non-bound label.
                                _eh.DisplacementSize = 4;
                                _eh.RelocationId = -1;
                                EmitDisplacement();
                                return;
                            }
                            break;
                        default:
                            // [RIP + Disp32].

                            // Indexing is invalid.
                            if (_eh.Index != -1) throw new InvalidDataException(string.Format("IllegalDisplacement: MemoryType is {0} and Index:{1} is invalid", _eh.ModRmMemory.MemoryType, _eh.Index));

                            EmitByte((byte)EncodeMod(0, _eh.Operand, 5));
                            EmitDWord(_eh.DisplacementOffset);
                            break;
                    }
                }
                else
                {
                    if (_eh.Index == -1 /*unchecked((uint)_eh.Index) >= unchecked((uint)-1)*/)
                    {
                        // [Disp32].
                        EmitByte((byte)EncodeMod(0, _eh.Operand, 5));
                    }
                    else
                    {
                        // [Index * Scale + Disp32].
                        var shift = _eh.ModRmMemory.Shift;
                        if (_eh.Index == RegisterIndex.Sp) throw new ArgumentException();

                        EmitByte((byte)EncodeMod(0, _eh.Operand, 4));
                        EmitByte((byte)EncodeSib(shift, _eh.Index, 5));
                    }

                    switch (_eh.ModRmMemory.MemoryType)
                    {
                        case MemoryType.Absolute:
                            // [Disp32].
                            EmitDWord(_eh.DisplacementOffset);
                            break;
                        case MemoryType.Label:
                            {
                                // Relative->Absolute [x86 mode].
                                _eh.Label = _assemblerBase.GetLabelData(_eh.ModRmMemory.Base);
                                _eh.RelocationId = _relocations.Count;

                                var rd = new RelocationData
                                {
                                    Mode = RelocationMode.RelToAbs,
                                    Size = 4,
                                    From = _cursor - _buffer,
                                    Data = (IntPtr)_eh.DisplacementOffset
                                };
                                _relocations.Add(rd);
                                if (_eh.Label.Offset != -1)
                                {
                                    // Bound label.
                                    _relocations[_eh.RelocationId].Data += _eh.Label.Offset;
                                    EmitDWord(0);
                                }
                                else
                                {
                                    // Non-bound label.
                                    _eh.DisplacementOffset = -4 - _eh.ImmediateLength;
                                    _eh.DisplacementSize = 4;
                                    EmitDisplacement();
                                    return;
                                }
                            }
                            break;
                        default:
                            {
                                // RIP->Absolute [x86 mode].
                                _eh.RelocationId = _relocations.Count;

                                var rd = new RelocationData
                                {
                                    Mode = RelocationMode.RelToAbs,
                                    Size = 4,
                                    From = _cursor - _buffer
                                };
                                rd.Data = rd.From + _eh.DisplacementOffset;

                                _relocations.Add(rd);

                                EmitDWord(0);
                            }
                            break;
                    }
                }
            }
            if (_eh.ImmediateLength == 0)
            {
                EmitDone();
                return;
            }

            EmitImm();
        }

        private void EmitFpuOp()
        {
            // Mandatory instruction prefix.
            EmitPp(_eh.OpCode);

            // Instruction opcodes.
            EmitOp(_eh.OpCode >> 8);
            EmitOp(_eh.OpCode);
            EmitDone();
        }

        private void EmitFpArith_Reg()
        {
            _eh.OpCode = 0xD800 + ((_eh.OpCode >> 8) & 0xFF) + _eh.ModRmRegister;
            EmitFpuOp();
        }

        private void EmitFpArith_Mem()
        {
            _eh.OpCode = _eh.Operand0.Size == 4 ? 0xD8u : 0xDC;
            _eh.ModRmMemory = OpMem(_eh.Operand0);
            EmitX86(false, true);
        }

        private void EmitMmMovD()
        {
            _eh.Operand = OpReg(_eh.Operand0);
            Add66Hp((OpRegType(_eh.Operand0) == RegisterType.Xmm).AsByte());

            // Mm/Xmm <- Gp
            if (OperandsAre(OperandType.Register, OperandType.Register) && _eh.Operand1.As<Register>().IsGp())
            {
                _eh.ModRmRegister = OpReg(_eh.Operand1);
                EmitX86(true);
                return;
            }

            // Mm/Xmm <- Mem
            if (OperandsAre(OperandType.Register, OperandType.Memory))
            {
                _eh.ModRmMemory = OpMem(_eh.Operand1);
                EmitX86(false, true);
                return;
            }

            // The following instructions use the secondary opcode.
            _eh.OpCode = _eh.SecondaryOpCode;
            _eh.Operand = OpReg(_eh.Operand1);
            Add66Hp((OpRegType(_eh.Operand1) == RegisterType.Xmm).AsByte());

            // Gp <- Mm/Xmm
            if (OperandsAre(OperandType.Register, OperandType.Register) && _eh.Operand0.As<Register>().IsGp())
            {
                _eh.ModRmRegister = OpReg(_eh.Operand0);
                EmitX86(true);
                return;
            }

            // X86Mem <- Mm/Xmm
            if (!OperandsAre(OperandType.Memory, OperandType.Register)) return;
            _eh.ModRmMemory = OpMem(_eh.Operand0);
            EmitX86(false, true);
        }

        private void AvxRmMr_AfterRegRegCheck()
        {
            if (OperandsAre(OperandType.Register, OperandType.Memory))
            {
                _eh.Operand = OpReg(_eh.Operand0);
                _eh.ModRmMemory = OpMem(_eh.Operand1);
                EmitAvxMAndSib();
                return;
            }

            // The following instruction uses the secondary opcode.
            _eh.OpCode &= Constants.X86.InstOpCode_L_Mask;
            _eh.OpCode |= _eh.SecondaryOpCode;

            if (!OperandsAre(OperandType.Memory, OperandType.Register)) return;
            _eh.Operand = OpReg(_eh.Operand1);
            _eh.ModRmMemory = OpMem(_eh.Operand0);
            EmitAvxMAndSib();
        }

        private void EmitDisplacement()
        {
            if (_eh.Label.Offset != -1) throw new ArgumentException();
            if (!(_eh.DisplacementSize == 1 || _eh.DisplacementSize == 4)) throw new ArgumentException();

            // Chain with label.
            var link = CreateLabelLink();
            link.Previous = _eh.Label.Links;
            link.Offset = Offset;
            link.Displacement = _eh.DisplacementOffset;
            link.RelocationId = _eh.RelocationId;
            _eh.Label.Links = link;

            // Emit label size as dummy data.
            if (_eh.DisplacementSize == 1) EmitByte(0x01);
            else EmitDWord(0x04040404);// if (dispSize == 4)

            if (_eh.ImmediateLength != 0)
            {
                EmitImm();
                return;
            }
            EmitDone();
        }

        private void EmitImm()
        {
            switch (_eh.ImmediateLength)
            {
                case 1:
                    EmitByte((byte)(_eh.ImmediateValue & 0x000000FF));
                    break;
                case 2:
                    EmitWord((ushort)(_eh.ImmediateValue & 0x0000FFFF));
                    break;
                case 4:
                    EmitDWord((int)(_eh.ImmediateValue & 0xFFFFFFFF));
                    break;
                case 8:
                    EmitQWord(_eh.ImmediateValue);
                    break;

                default:
                    throw new ArgumentException(string.Format("unknown immediateLength: {0}", _eh.ImmediateLength));
            }
            EmitDone();
        }

        private void EmitX86OpWithOpReg()
        {
            // Mandatory instruction prefix.
            EmitPp(_eh.OpCode);

            if (Constants.X64)
            {
                // Rex prefix (64-bit only).
                var rex = RexFromOpCodeAndOptions(_eh.OpCode, _eh.InstructionOptions);

                rex += _eh.Operand >> 3; // Rex.B (0x01).

                if ((rex & ~Constants.X86._InstOptionNoRex) != 0)
                {
                    rex |= Constants.X86.ByteRex;
                    _eh.Operand &= 0x07;
                    EmitByte((byte)rex);

                    if (rex >= Constants.X86._InstOptionNoRex) throw new ArgumentException();
                }
            }

            // Instruction opcodes.
            _eh.OpCode += _eh.Operand;
            EmitMm(_eh.OpCode);
            EmitOp(_eh.OpCode);

            if (_eh.ImmediateLength != 0) EmitImm();
            else EmitDone();
        }

        private void EmitJmpOrCallAbs()
        {
            var rd = new RelocationData
            {
                Mode = RelocationMode.AbsToRel,
                Size = 4,
                From = _cursor - _buffer + 1,
                Data = (IntPtr)_eh.ImmediateValue
            };

            var trampolineSize = 0;

            if (Constants.X64)
            {
                var baseAddress = _assemblerBase.BaseAddress;

                // If the base address of the output is known, it's possible to determine
                // the need for a trampoline here. This saves possible REX prefix in
                // 64-bit mode and prevents reserving space needed for an absolute address.
                if (baseAddress == Pointer.Invalid || !((long)(rd.Data - baseAddress + rd.From + 4)).IsInt32())
                {
                    // Emit REX prefix so the instruction can be patched later on. The REX
                    // prefix does nothing if not patched after, but allows to patch the
                    // instruction in case where the trampoline is needed.
                    rd.Mode = RelocationMode.Trampoline;
                    rd.From += 1;

                    EmitByte(Constants.X86.ByteRex);
                    trampolineSize = 8;
                }
            }

            // Both `jmp` and `call` instructions have a single-byte opcode and are
            // followed by a 32-bit displacement.
            EmitOp(_eh.OpCode);
            EmitDWord(0);

            _relocations.Add(rd);
            // Reserve space for a possible trampoline.
            _trampolinesSize += trampolineSize;

            EmitDone();
        }

        private void GroupPop_Gp()
        {
            // We allow 2 byte, 4 byte, and 8 byte register sizes, althought PUSH
            // and POP only allows 2 bytes or register width. On 64-bit we simply
            // PUSH/POP 64-bit register even if 32-bit register was given.
            if (_eh.Operand0.Size < 1) throw new ArgumentException();

            _eh.OpCode = _eh.SecondaryOpCode;
            _eh.Operand = OpReg(_eh.Operand0);

            Add66HpBySize(_eh.Operand0.Size);
            EmitX86OpWithOpReg();
        }

        private void Add66Hp(byte value) => _eh.OpCode |= (uint)(value << (int)Constants.X86.InstOpCode_PP_Shift);

        private void AddRexW(int value)
        {
            if (Constants.X64) _eh.OpCode |= (uint)(value << (int)Constants.X86.InstOpCode_W_Shift);
        }

        private void AddRexWBySize(int value)
        {
            if (Constants.X64)
            {
                if (value == 8) _eh.OpCode |= Constants.X86.InstOpCode_W;
            }
        }

        private void Add66HpBySize(int value) => _eh.OpCode |= (uint)(value & 0x02) << (int)(Constants.X86.InstOpCode_PP_Shift - 1);

        private void AddVexL(byte value) => _eh.OpCode |= (uint)(value << (int)Constants.X86.InstOpCode_L_Shift);

        private void AddVexW(byte value) => _eh.OpCode |= (uint)(value << (int)Constants.X86.InstOpCode_W_Shift);

        private void EmitDone() { }

        public static long EncodeMod(long m, long o, long rm)
        {
            if (!(m <= 3)) throw new ArgumentException();
            if (!(o <= 7)) throw new ArgumentException();
            if (!(rm <= 7)) throw new ArgumentException();
            return (m << 6) + (o << 3) + rm;
        }

        //! Encode SIB.
        public static long EncodeSib(long s, long i, long b)
        {
            if (!(s <= 3)) throw new ArgumentException();
            if (!(i <= 7)) throw new ArgumentException();
            if (!(b <= 7)) throw new ArgumentException();
            return (s << 6) + (i << 3) + b;
        }

        public static long ExtractO(long opcode) => (opcode >> (int)Constants.X86.InstOpCode_O_Shift) & 0x07;

        public static long RegAndVvvv(long regIndex, long vvvvIndex) => regIndex + (vvvvIndex << (int)Constants.X86.VexVVVVShift);

        public static long RexFromOpCodeAndOptions(long opCode, InstructionOptions options)
        {
            var rex = opCode >> (int)(Constants.X86.InstOpCode_W_Shift - 3);
            if (!((rex & ~0x08) == 0)) throw new ArgumentException();

            return rex + (long)(options & InstructionOptions.NoRexMask);
        }

        public static Memory OpMem(Operand op) => op.As<Memory>();

        public static int OpReg(Operand op) => op.Reserved0; //As<Register>().RegisterIndex;

        public static RegisterType OpRegType(Operand op) => (RegisterType)op.Reserved1; //As<Register>().RegisterType;
    }
}
