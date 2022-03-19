using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocStack : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var stack = c.Stack(256, 1).SetSize(1);
            var i = c.IntPtr("i");
            var a = c.Int32("a");
            var b = c.Int32("b");

            var l1 = c.Label();
            var l2 = c.Label();

            // Fill stack by sequence [0, 1, 2, 3 ... 255].
            c.Emit(InstructionId.Xor, i, i);

            c.Bind(l1);
            c.Emit(InstructionId.Mov, stack.Clone().SetIndex(i), i.As8());
            c.Emit(InstructionId.Inc, i);
            c.Emit(InstructionId.Cmp, i, (Immediate)255);
            c.Emit(InstructionId.Jle, l1);

            // Sum sequence in stack.
            c.Emit(InstructionId.Xor, i, i);
            c.Emit(InstructionId.Xor, a, a);

            c.Bind(l2);
            c.Emit(InstructionId.Movzx, b, stack.Clone().SetIndex(i));
            c.Emit(InstructionId.Add, a, b);
            c.Emit(InstructionId.Inc, i);
            c.Emit(InstructionId.Cmp, i, (Immediate)255);
            c.Emit(InstructionId.Jle, l2);

            c.Ret(a);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = 32640.ToString();
        }
    }
}