using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using CodeContext = AsmJit.CompilerContext.CodeContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallMisc1 : CompilerTestCase<Func<int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var a1 = c.Int32("a1");
            var a2 = c.Int32("a2");
            var a3 = c.Int32("a3");
            var r = c.Int32("r");

            c.SetArgument(a1, a2, a3);
            c.Allocate(a1, Cpu.Registers.Eax);
            c.Allocate(a2, Cpu.Registers.Ebx);
            c.Allocate(a3, Cpu.Registers.Ecx);

            //var call = c.Call(FnPointer.Fn(new Action<int, int>(CalledFunction)));
            //call.SetArgument(a);
            //call.SetArgument(b);

            c.Emit(
                Inst.Lea, r, Memory.Ptr(a1, a2),
                Inst.Lea, r, Memory.Ptr(r, a3));
            c.Ret(r);
        }

        protected override void Execute(Func<int, int, int, int> fn, out string result, out string expected)
        {
            result = fn(44, 199, 10).ToString();
            expected = (44+199+10).ToString();
        }

        //private static void CalledFunction(int a, int b) {  }
    }
}