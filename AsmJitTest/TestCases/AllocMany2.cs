using System;
using AsmJit.Common;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocMany2 : CompilerTestCase<Action<IntPtr>>
    {
        protected override void Compile(CodeContext c)
        {
            var var = new GpVariable[32];

            var a = c.SetArgument(c.IntPtr("a"));

            int i;
            for (i = 0; i < var.Length; i++)
            {
                var[i] = c.Int32("var" + i);
            }

            for (i = 0; i < var.Length; i++)
            {
                c.Emit(Inst.Xor, var[i], var[i]);
            }

            var v0 = c.Int32("v0");
            var l = c.Label();

            c.Emit(Inst.Mov, v0, (Immediate)32);
            c.Bind(l);

            for (i = 0; i < var.Length; i++)
            {
                c.Emit(Inst.Add, var[i], (Immediate)i);
            }

            c.Emit(Inst.Dec, v0);
            c.Emit(Inst.Jnz, l);

            for (i = 0; i < var.Length; i++)
            {
                c.Emit(Inst.Mov, Memory.DWord(a, i * 4), var[i]);
            }
        }

        protected override void Execute(Action<IntPtr> fn, out string result, out string expected)
        {
            var x = new int[32];
            unsafe
            {

                fixed (int* px = x)
                {
                    fn((IntPtr) px);
                }
            }
            result = string.Join(",", x);
            expected = string.Join(",", new int[32].InitializeWith(i => i * 32));
        }
    }
}