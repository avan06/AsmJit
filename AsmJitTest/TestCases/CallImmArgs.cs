using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallImmArgs : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var fn = c.IntPtr("fn");
            var rv = c.Int32("rv");

            var fp = FnPointer.Fn(new Func<int, int, int, int, int, int, int, int, int, int, int>(CalledFunction));
            c.Emit(Inst.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument((Immediate)0x03, 0x12, 0xA0, 0x0B, 0x2F, 0x02, 0x0C, 0x12, 0x18, 0x1E);
            call.SetReturn(rv);

            c.Ret(rv);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = CalledFunction(0x03, 0x12, 0xA0, 0x0B, 0x2F, 0x02, 0x0C, 0x12, 0x18, 0x1E).ToString();
        }

        private static int CalledFunction(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
        {
            return (a * b * c * d * e) + (f * g * h * i * j);
        }
    }
}