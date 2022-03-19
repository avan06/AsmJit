using System;
using AsmJit.Common;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocUseMem : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var l1 = c.Label();

            var iIdx = c.Int32("iIdx");
            var iEnd = c.Int32("iEnd");

            var aIdx = c.SetArgument(c.Int32("aIdx"));
            var aEnd = c.SetArgument(c.Int32("aEnd"));

            c.Emit(InstructionId.Mov, iIdx, aIdx);
            c.Emit(InstructionId.Mov, iEnd, aEnd);
            c.Spill(iEnd);

            c.Bind(l1);
            c.Emit(InstructionId.Inc, iIdx);
            c.Emit(InstructionId.Cmp, iIdx, iEnd.ToMemory());
            c.Emit(InstructionId.Jne, l1);

            c.Ret(iIdx);
        }

        protected override void Execute(Func<int, int, int> fn, out string result, out string expected)
        {
            result = fn(10, 20).ToString();
            expected = 20.ToString();
        }
    }
}