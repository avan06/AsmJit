using System;
using AsmJit.Common.Enums;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class JumpCross : CompilerTestCase<Action>
    {
        protected override void Compile(CodeContext c)
        {
            var l1 = c.Label();
            var l2 = c.Label();
            var l3 = c.Label();

            c.Emit(InstructionId.Jmp, l2);

            c.Bind(l1);
            c.Emit(InstructionId.Jmp, l3);

            c.Bind(l2);
            c.Emit(InstructionId.Jmp, l1);

            c.Bind(l3);

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
