using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocBase : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");
            var v2 = c.Int32("v2");
            var v3 = c.Int32("v3");
            var v4 = c.Int32("v4");

            c.Emit(InstructionId.Xor, v0, v0);

            c.Emit(
                InstructionId.Mov, v1, (Immediate)1,
                InstructionId.Mov, v2, (Immediate)2,
                InstructionId.Mov, v3, (Immediate)3,
                InstructionId.Mov, v4, (Immediate)4);

            c.Emit(
                InstructionId.Add, v0, v1,
                InstructionId.Add, v0, v2,
                InstructionId.Add, v0, v3,
                InstructionId.Add, v0, v4);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (1 + 2 + 3 + 4).ToString();
        }
    }
}