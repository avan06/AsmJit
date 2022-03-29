using System;
using AsmJit.AssemblerContext;

namespace AsmJitTest.TestCases
{
    public sealed class RawAssembler : AssemblerTestCase<Func<int, int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            //var R = Cpu.Registers;
            //c.Emit(Inst.Push, R.Rbx);
            //c.Emit(Inst.Mov, R.Eax, R.Ecx);
            //c.Emit(Inst.Mov, R.Ebx, R.Edx);
            //c.Emit(Inst.Lea, R.Ecx, Memory.DWord(R.Eax, R.Ebx));
            //c.Emit(Inst.Lea, R.Edx, Memory.DWord(R.R8D, R.R9D));
            //c.Emit(Inst.Lea, R.Ecx, Memory.DWord(R.Ecx, R.Edx));
            //c.Emit(Inst.Mov, R.Eax, R.Ecx);
            //c.Emit(Inst.Pop, R.Rbx);
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
