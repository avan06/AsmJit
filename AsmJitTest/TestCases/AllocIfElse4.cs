using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocIfElse4 : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v1 = c.SetArgument(c.Int32("v1"));
            var v2 = c.SetArgument(c.Int32("v2"));

            var counter = c.Int32("counter");

            var l1 = c.Label();
            var loop1 = c.Label();
            var loop2 = c.Label();
            var exit = c.Label();

            c.Emit(
                InstructionId.Mov, counter, (Immediate)0,

                InstructionId.Cmp, v1, v2,
                InstructionId.Jg, l1);

            c.Bind(loop1);
            c.Emit(
                InstructionId.Mov, v1, counter,
                InstructionId.Inc, counter,
                InstructionId.Cmp, counter, (Immediate)1,
                InstructionId.Jle, loop1,
                InstructionId.Jmp, exit);

            c.Bind(l1);
            c.Bind(loop2);
            c.Emit(
                InstructionId.Mov, v1, counter,
                InstructionId.Inc, counter,
                InstructionId.Cmp, counter, (Immediate)2,
                InstructionId.Jle, loop2);

            c.Bind(exit);
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