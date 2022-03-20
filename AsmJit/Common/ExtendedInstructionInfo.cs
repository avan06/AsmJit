using System;
using AsmJit.Common.Enums;

namespace AsmJit.Common
{
    internal class ExtendedInstructionInfo
    {
        public readonly InstructionEncoding Encoding;

        public readonly long WriteSize;

        public readonly long[] OperandFlags;

        public readonly long InstructionFlags;

        public readonly long SecondaryOpCode;

        public ExtendedInstructionInfo(InstructionEncoding encoding, long[] operandFlags = null, long instructionFlags = Constants.X86.InstFlagNone, long writeSize = 0, long secop = 0)
        {
            if (operandFlags == null) operandFlags = new[] { 0L, 0, 0, 0, 0 };
            else if (operandFlags.Length < 5) Array.Resize(ref operandFlags, 5);
            Encoding = encoding;
            OperandFlags = operandFlags;
            InstructionFlags = instructionFlags;
            WriteSize = writeSize;
            SecondaryOpCode = secop;
        }
    }
}