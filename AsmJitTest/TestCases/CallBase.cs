using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallBase : CompilerTestCase<Func<int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");
            var v2 = c.Int32("v2");

            c.SetArgument(v0);
            c.SetArgument(v1);
            c.SetArgument(v2);

            var fp = Memory.Fn(new Func<int, int, int, int>(CalledFunction));

            // Just do something.
            c.Emit(InstructionId.Shl, v0, (Immediate)1);
            c.Emit(InstructionId.Shl, v1, (Immediate)1);
            c.Emit(InstructionId.Shl, v2, (Immediate)1);

            var fn = c.IntPtr("fn");
            c.Emit(InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument(v2);
            call.SetArgument(v1);
            call.SetArgument(v0);
            call.SetReturn(v0);

            c.Ret(v0);
        }

        protected override void Execute(Func<int, int, int, int> fn, out string result, out string expected)
        {
            result = fn(3, 2, 1).ToString();
            expected = 36.ToString();
        }

        private static int CalledFunction(int a, int b, int c) => (a + b) * c;
    }
}