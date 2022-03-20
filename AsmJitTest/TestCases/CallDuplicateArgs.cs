using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallDuplicateArgs : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var fn = c.IntPtr("fn");
            var a = c.Int32("a");

            var fp = FnPointer.Fn(new Func<int, int, int, int, int, int, int, int, int, int, int>(CalledFunction));
            c.Emit(
                InstructionId.Mov, fn, fp,
                InstructionId.Mov, a, (Immediate)3);

            var call = c.Call(fn, fp);
            call.SetArgument(a, a, a, a, a, a, a, a, a, a);
            call.SetReturn(a);

            c.Ret(a);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = CalledFunction(3, 3, 3, 3, 3, 3, 3, 3, 3, 3).ToString();
        }

        private static int CalledFunction(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
        {
            return (a * b * c * d * e) + (f * g * h * i * j);
        }
    }
}