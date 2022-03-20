using System;
using System.Globalization;
using AsmJit.Common.Enums;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallDoubleAsXmmRet : CompilerTestCase<Func<double, double, double>>
    {
        protected override void Compile(CodeContext c)
        {
            //0000024c74640000 53                             push rbx
            //0000024c74640001 48 83 ec 20                    sub rsp, 0x20
            //0000024c74640005 48 bb ec 0d 53 74 4c 02 00 00  mov rbx, 0x24c74530dec
            //0000024c7464000f ff d3                          call rbx
            //0000024c74640011 48 83 c4 20                    add rsp, 0x20
            //0000024c74640015 5b                             pop rbx
            //0000024c74640016 c3                             ret
            var fn = c.IntPtr("fn");
            var a = c.SetArgument(c.XmmSd("a"));
            var b = c.SetArgument(c.XmmSd("b"));
            var ret = c.XmmSd("ret");

            var fp = FnPointer.Fn(new Func<double, double, double>(CalledFunction));
            c.Emit(InstructionId.Mov, fn, fp);

            var call = c.Call(fn, fp);
            call.SetArgument(a);
            call.SetArgument(b);
            call.SetReturn(ret);

            c.Ret(ret);
        }

        protected override void Execute(Func<double, double, double> fn, out string result, out string expected)
        {
            result = fn(15.5, 2.0).ToString(CultureInfo.InvariantCulture);
            expected = CalledFunction(15.5, 2.0).ToString(CultureInfo.InvariantCulture);
        }

        private static double CalledFunction(double a, double b) => a * b;
    }
}