using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;

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

        public bool IsFlow() => InstructionFlags.IsSet(Constants.X86.InstFlagFlow);

        public bool IsTest() => InstructionFlags.IsSet(Constants.X86.InstFlagTest);

        public bool IsMove() => InstructionFlags.IsSet(Constants.X86.InstFlagMove);

        public bool IsXchg() => InstructionFlags.IsSet(Constants.X86.InstFlagXchg);

        public bool IsFp() => InstructionFlags.IsSet(Constants.X86.InstFlagFp);

        public bool IsLockable() => InstructionFlags.IsSet(Constants.X86.InstFlagLock);

        public bool IsSpecial() => InstructionFlags.IsSet(Constants.X86.InstFlagSpecial);

        public bool IsSpecialMem() => InstructionFlags.IsSet(Constants.X86.InstFlagSpecialMem);

        public bool IsZeroIfMem() => InstructionFlags.IsSet(Constants.X86.InstFlagZ);
    }
}