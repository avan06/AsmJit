using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallPtrArgs : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var fn = c.IntPtr("fn");
            var fp = Memory.Fn(new Func<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>(CalledFunction));
            c.Emit(InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument((Immediate)1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var rv = call.SetReturn(c.Int32("rv"));

            c.Ret(rv);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = 55.ToString();
        }

        private static int CalledFunction(IntPtr a, IntPtr b, IntPtr c, IntPtr d, IntPtr e, IntPtr f, IntPtr g, IntPtr h, IntPtr i, IntPtr j)
        {
            return (a.ToInt32() + b.ToInt32() + c.ToInt32() + d.ToInt32() + e.ToInt32()) + (f.ToInt32() + g.ToInt32() + h.ToInt32() + i.ToInt32() + j.ToInt32());
        }
    }
}