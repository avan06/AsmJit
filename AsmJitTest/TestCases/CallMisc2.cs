using System;
using System.Globalization;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallMisc2 : CompilerTestCase<Func<IntPtr, double>>
    {
        protected override void Compile(CodeContext c)
        {
            var p = c.SetArgument(c.IntPtr("p"));
            var fn = c.IntPtr("fn");

            var arg = c.XmmSd("arg");
            var ret = c.XmmSd("ret");

            var fp = FnPointer.Fn(new Func<double, double>(CalledFunction));

            c.Emit(
                InstructionId.Movsd, arg, Memory.Ptr(p),
                InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument(arg);
            call.SetReturn(ret);

            c.Ret(ret);
        }

        protected override void Execute(Func<IntPtr, double> fn, out string result, out string expected)
        {
            var a = 2.0;
            unsafe
            {
                result = fn((IntPtr)(&a)).ToString(CultureInfo.InvariantCulture);
            }
            expected = CalledFunction(a).ToString(CultureInfo.InvariantCulture);
        }

        private static double CalledFunction(double a)
        {
            return a*a;
        }
    }
}