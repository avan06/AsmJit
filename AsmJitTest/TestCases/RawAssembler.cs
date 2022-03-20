using System;
using AsmJit.AssemblerContext;

namespace AsmJitTest.TestCases
{
    public sealed class RawAssembler : AssemblerTestCase<Func<int, int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            //var R = Cpu.Registers;
            //c.Emit(InstructionId.Push, R.Rbx);
            //c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
            //c.Emit(InstructionId.Mov, R.Ebx, R.Edx);
            //c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Eax, R.Ebx));
            //c.Emit(InstructionId.Lea, R.Edx, Memory.DWord(R.R8D, R.R9D));
            //c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Ecx, R.Edx));
            //c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
            //c.Emit(InstructionId.Pop, R.Rbx);
            //c.Ret();
            c.Emit(@"
Push Rbx
Mov Eax, Ecx
Mov Ebx, Edx
");
            c.Emit(@"
lea ecx, [eax+ebx]
lea edx, [r8d+r9d]
lea ecx, [ecx+edx]
");
            c.Emit(@"
mov eax, ecx
pop rbx
ret
");
        }

        protected override void Execute(Func<int, int, int, int, int> fn, out string result, out string expected)
        {
            result = fn(12, 24, 36, 48).ToString();
            expected = (12 + 24 + 36 + 48).ToString();
        }
    }
}
