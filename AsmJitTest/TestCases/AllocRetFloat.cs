using System;
using System.Globalization;
using AsmJit.Common.Enums;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocRetFloat : CompilerTestCase<Func<float, float, float>>
    {
        protected override void Compile(CodeContext c)
        {
            var a = c.SetArgument(c.XmmSs("a"));
            var b = c.SetArgument(c.XmmSs("b"));

            c.Emit(InstructionId.Addss, a, b);
            c.Ret(a);
        }

        protected override void Execute(Func<float, float, float> fn, out string result, out string expected)
        {
            result = fn(1.0f, 2.0f).ToString(CultureInfo.InvariantCulture);
            expected = (1.0f + 2.0f).ToString(CultureInfo.InvariantCulture);
        }
    }
}