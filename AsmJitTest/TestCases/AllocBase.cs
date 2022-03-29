using System;
using AsmJit.Common;
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

            c.Emit(Inst.Xor, v0, v0);

            c.Emit(
                Inst.Mov, v1, (Immediate)1,
                Inst.Mov, v2, (Immediate)2,
                Inst.Mov, v3, (Immediate)3,
                Inst.Mov, v4, (Immediate)4);

            c.Emit(
                Inst.Add, v0, v1,
                Inst.Add, v0, v2,
                Inst.Add, v0, v3,
                Inst.Add, v0, v4);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (1 + 2 + 3 + 4).ToString();
        }
    }
}