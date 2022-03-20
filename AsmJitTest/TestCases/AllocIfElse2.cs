using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocIfElse2 : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v1 = c.SetArgument(c.Int32("v1"));
            var v2 = c.SetArgument(c.Int32("v2"));

            var l1 = c.Label();
            var l2 = c.Label();
            var l3 = c.Label();
            var l4 = c.Label();

            c.Emit(InstructionId.Jmp, l1);
            c.Bind(l2);
            c.Emit(InstructionId.Jmp, l4);
            c.Bind(l1);

            c.Emit(
                InstructionId.Cmp, v1, v2,
                InstructionId.Jg, l3,

                InstructionId.Mov, v1, (Immediate)1,
                InstructionId.Jmp, l2);

            c.Bind(l3);
            c.Emit(
                InstructionId.Mov, v1, (Immediate)2,
                InstructionId.Jmp, l2);

            c.Bind(l4);

            c.Ret(v1);
        }

        protected override void Execute(Func<int, int, int> fn, out string result, out string expected)
        {
            var r1 = fn(0, 1);
            var r2 = fn(1, 0);

            result = r1 + " " + r2;
            expected = 1 + " " + 2;
        }
    }
}