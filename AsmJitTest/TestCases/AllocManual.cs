using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocManual : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");
            var cnt = c.Int32("cnt");

            c.Emit(InstructionId.Xor, v0, v0);
            c.Emit(InstructionId.Xor, v1, v1);
            c.Spill(v0);
            c.Spill(v1);

            var l = c.Label();
            c.Emit(InstructionId.Mov, cnt, (Immediate)32);
            c.Bind(l);

            c.Emit(InstructionId.Inc, v1);
            c.Emit(InstructionId.Add, v0, v1);

            c.Emit(InstructionId.Dec, cnt);
            c.Emit(InstructionId.Jnz, l);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32).ToString();
        }
    }
}