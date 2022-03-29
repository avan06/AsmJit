using System;
using AsmJit.Common;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocIdiv1 : CompilerTestCase<Func<IntPtr, IntPtr, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var a = c.SetArgument(c.IntPtr("a"));
            var b = c.SetArgument(c.IntPtr("b"));

            var dummy = c.Int32("dummy");

            c.Emit(
                Inst.Xor, dummy, dummy,
                Inst.Idiv, dummy, a, b);
            c.Ret(a);
        }

        protected override void Execute(Func<IntPtr, IntPtr, int> fn, out string result, out string expected)
        {
            var v0 = 2999;
            var v1 = 245;
            result = fn((IntPtr)v0, (IntPtr)v1).ToString();
            expected = (2999/245).ToString();
        }
    }
}