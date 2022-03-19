using System;
using System.Globalization;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallFloatAsXmmRet : CompilerTestCase<Func<float, float, float>>
    {
        protected override void Compile(CodeContext c)
        {
            var fn = c.IntPtr("fn");
            var a = c.SetArgument(c.XmmSs("a"));
            var b = c.SetArgument(c.XmmSs("b"));
            var ret = c.XmmSs("ret");

            var fp = Memory.Fn(new Func<float, float, float>(CalledFunction));
            c.Emit(InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument(a);
            call.SetArgument(b);
            call.SetReturn(ret);

            c.Ret(ret);
        }

        protected override void Execute(Func<float, float, float> fn, out string result, out string expected)
        {
            result = fn(15.5f, 2.0f).ToString(CultureInfo.InvariantCulture);
            expected = CalledFunction(15.5f, 2.0f).ToString(CultureInfo.InvariantCulture);
        }

        private static float CalledFunction(float a, float b)
        {
            return a * b;
        }
    }
}