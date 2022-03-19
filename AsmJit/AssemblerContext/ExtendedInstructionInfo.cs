using AsmJit.Common;
using System;

namespace AsmJit.AssemblerContext
{
    internal class ExtendedInstructionInfo
    {
        public ExtendedInstructionInfo(InstructionEncoding encoding) : this(encoding, 0, 0, 0, 0, 0, null, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long[] operandFlags) : this(encoding, 0, 0, 0, 0, 0, operandFlags, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long instructionFlags) : this(encoding, 0, 0, 0, 0, 0, null, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long[] operandFlags, long instructionFlags) : this(encoding, 0, 0, 0, 0, 0, operandFlags, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long[] operandFlags, long instructionFlags, long secop) : this(encoding, 0, 0, 0, 0, 0, operandFlags, instructionFlags, secop) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsOut, long[] operandFlags) : this(encoding, 0, 0, 0, eflagsOut, 0, operandFlags, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsOut, long[] operandFlags, long instructionFlags) : this(encoding, 0, 0, 0, eflagsOut, 0, operandFlags, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsOut, long[] operandFlags, long instructionFlags, long secop) : this(encoding, 0, 0, 0, eflagsOut, 0, operandFlags, instructionFlags, secop) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsIn, long eflagsOut) : this(encoding, 0, 0, eflagsIn, eflagsOut, 0, null, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsIn, long eflagsOut, long[] operandFlags) : this(encoding, 0, 0, eflagsIn, eflagsOut, 0, operandFlags, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsIn, long eflagsOut, long instructionFlags) : this(encoding, 0, 0, eflagsIn, eflagsOut, 0, null, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long eflagsIn, long eflagsOut, long[] operandFlags, long instructionFlags) : this(encoding, 0, 0, eflagsIn, eflagsOut, 0, operandFlags, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long writeIndex, long writeSize, long eflagsIn, long eflagsOut, long dummy, long[] operandFlags) : this(encoding, writeIndex, writeSize, eflagsIn, eflagsOut, dummy, operandFlags, Constants.X86.InstFlagNone, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long writeIndex, long writeSize, long eflagsIn, long eflagsOut, long dummy, long instructionFlags) : this(encoding, writeIndex, writeSize, eflagsIn, eflagsOut, dummy, null, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long writeIndex, long writeSize, long eflagsIn, long eflagsOut, long dummy, long[] operandFlags, long instructionFlags) : this(encoding, writeIndex, writeSize, eflagsIn, eflagsOut, dummy, operandFlags, instructionFlags, 0) { }
        public ExtendedInstructionInfo(InstructionEncoding encoding, long writeIndex, long writeSize, long eflagsIn, long eflagsOut, long dummy, long[] operandFlags, long instructionFlags, long secop)
        {
            if (operandFlags == null) operandFlags = new[] { 0L, 0, 0, 0, 0 };
            else if (operandFlags.Length < 5) Array.Resize(ref operandFlags, 5);
            Encoding = encoding;
            WriteIndex = writeIndex;
            WriteSize = writeSize;
            EflagsIn = eflagsIn;
            EflagsOut = eflagsOut;
            Reserved = dummy;
            OperandFlags = operandFlags;
            InstructionFlags = instructionFlags;
            SecondaryOpCode = secop;
        }

        public InstructionEncoding Encoding { get; private set; }

        public long WriteIndex { get; private set; }

        public long WriteSize { get; private set; }

        public long EflagsIn { get; private set; }

        public long EflagsOut { get; private set; }

        public long Reserved { get; private set; }

        public long[] OperandFlags { get; private set; }

        public long InstructionFlags { get; private set; }

        public long SecondaryOpCode { get; private set; }
    }
}