using System;
using AsmJit.AssemblerContext;
using AsmJit.Common;
using AsmJit.Common.Operands;
using R = AsmJit.AssemblerContext.Cpu.Registers;

namespace AsmJitTest.TestCases
{
    public sealed class RawAssembler : AssemblerTestCase<Func<int, int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            c.Emit(InstructionId.Push, R.Rbx);
            c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
            c.Emit(InstructionId.Mov, R.Ebx, R.Edx);
            c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Eax, R.Ebx)); //lea ecx, [eax+ebx]
            c.Emit(InstructionId.Lea, R.Edx, Memory.DWord(R.R8D, R.R9D)); //lea edx, [r8d+r9d]
            c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Ecx, R.Edx)); //lea ecx, [ecx+edx]
            c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
            c.Emit(InstructionId.Pop, R.Rbx);
            c.Ret();
        }

        protected override void Execute(Func<int, int, int, int, int> fn, out string result, out string expected)
        {
            result = fn(12, 24, 36, 48).ToString();
            expected = (12 + 24 + 36 + 48).ToString();
        }
    }
}
