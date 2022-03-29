using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocIfElse3 : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v1 = c.SetArgument(c.Int32("v1"));
            var v2 = c.SetArgument(c.Int32("v2"));

            var counter = c.Int32("counter");

            var l1 = c.Label();
            var loop = c.Label();
            var exit = c.Label();

            c.Emit(
                Inst.Cmp, v1, v2,
                Inst.Jg, l1,

                Inst.Mov, counter, (Immediate)0);

            c.Bind(loop);
            c.Emit(
                Inst.Mov, v1, counter,

                Inst.Inc, counter,
                Inst.Cmp, counter, (Immediate)1,
                Inst.Jle, loop,
                Inst.Jmp, exit);

            c.Bind(l1);
            c.Emit(Inst.Mov, v1, (Immediate)2);

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