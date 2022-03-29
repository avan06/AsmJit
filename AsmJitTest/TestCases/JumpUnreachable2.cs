using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class JumpUnreachable2 : CompilerTestCase<Action>
    {
        protected override void Compile(CodeContext c)
        {
            var l1 = c.Label();
            var l2 = c.Label();

            var v0 = c.UInt32("v0");
            var v1 = c.UInt32("v1");

            c.Emit(Inst.Jmp, l1);
            c.Bind(l2);
            c.Emit(
                Inst.Mov, v0, (Immediate)1,
                Inst.Mov, v1, (Immediate)2,
                Inst.Cmp, v0, v1,
                Inst.Jz, l2,
                Inst.Jmp, l1);

            c.Bind(l1);
            c.Ret();
        }

        protected override void Execute(Action fn, out string result, out string expected)
        {
            result = string.Empty;
            expected = string.Empty;
            fn();
        }
    }
}