using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using CodeContext = AsmJit.CompilerContext.CodeContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallRecursive : CompilerTestCase<Func<int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var val = c.Int32("val");
            var skip = c.Label();

            c.SetArgument(0, val);

            c.Emit(
                InstructionId.Cmp, val, (Immediate)1,
                InstructionId.Jle, skip);

            var tmp = c.Int32("tmp");
            c.Emit(
                InstructionId.Mov, tmp, val,
                InstructionId.Dec, tmp);

            var call = c.Call(c.Entry);
            call.SetArgument(0, tmp);
            call.SetReturn(0, tmp);
            c.Emit(InstructionId.Mul, c.Int32(), val, tmp);

            c.Bind(skip);
            c.Ret(val);
        }

        protected override void Execute(Func<int, int> fn, out string result, out string expected)
        {
            result = fn(5).ToString();
            expected = (1*2*3*4*5).ToString();
        }
    }
}