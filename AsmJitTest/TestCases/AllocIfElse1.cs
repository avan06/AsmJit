using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocIfElse1 : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v1 = c.SetArgument(c.Int32("v1"));
            var v2 = c.SetArgument(c.Int32("v2"));

            var l1 = c.Label();
            var l2 = c.Label();

            c.Emit(InstructionId.Cmp, v1, v2);
            c.Emit(InstructionId.Jg, l1);

            c.Emit(InstructionId.Mov, v1, (Immediate)1);
            c.Emit(InstructionId.Jmp, l2);

            c.Bind(l1);
            c.Emit(InstructionId.Mov, v1, (Immediate)2);

            c.Bind(l2);
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