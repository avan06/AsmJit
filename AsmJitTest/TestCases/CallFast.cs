using System;
using AsmJit.Common.Enums;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallFast : CompilerTestCase<Func<int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var var = c.SetArgument(c.Int32("var"));
            var fn = c.IntPtr("fn");

            var fp = FnPointer.Fn(new Func<int, int>(CalledFunction)/*, CallingConvention.HostDefaultFast*/);
            c.Emit(InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument(var);
            call.SetReturn(var);

            call = c.Call(fn, fp);
            call.SetArgument(var);
            call.SetReturn(var);

            c.Ret(var);
        }

        protected override void Execute(Func<int, int> fn, out string result, out string expected)
        {
            result = fn(9).ToString();
            expected = ((9*9)*(9*9)).ToString();
        }

        private static int CalledFunction(int a)
        {
            return a * a;
        }
    }
}