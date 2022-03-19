using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocMemcpy : CompilerTestCase<Action<IntPtr, IntPtr, UIntPtr>>
    {
        protected override void Compile(CodeContext c)
        {
            var dst = c.SetArgument(c.IntPtr("dst"));
            var src = c.SetArgument(c.IntPtr("src"));
            var cnt = c.SetArgument(c.UIntPtr("cnt"));

            var loop = c.Label(); // Create base labels we use
            var exit = c.Label(); // in our function.

            c.Allocate(dst); // Allocate all registers now,
            c.Allocate(src); // because we want to keep them
            c.Allocate(cnt); // in physical registers only.

            c.Emit(InstructionId.Test, cnt, cnt); // Exit if length is zero.
            c.Emit(InstructionId.Jz, exit);

            c.Bind(loop); // Bind the loop label here.

            var tmp = c.Int32("tmp"); // Copy a single dword (4 bytes).
            c.Emit(InstructionId.Mov, tmp, Memory.DWord(src));
            c.Emit(InstructionId.Mov, Memory.DWord(dst), tmp);

            c.Emit(InstructionId.Add, src, (Immediate)4); // Increment dst/src pointers.
            c.Emit(InstructionId.Add, dst, (Immediate)4);

            c.Emit(InstructionId.Dec, cnt); // Loop until cnt isn't zero.
            c.Emit(InstructionId.Jnz, loop);

            c.Bind(exit); // Bind the exit label here.
        }

        protected override void Execute(Action<IntPtr, IntPtr, UIntPtr> fn, out string result, out string expected)
        {
            unsafe
            {
                const int cnt = 32;
                var a = new int[cnt].InitializeWith(i => i);
                var b = new int[cnt];

                fixed (int* src = a)
                {
                    fixed (int* dst = b)
                    {
                        fn((IntPtr)dst, (IntPtr)src, (UIntPtr)cnt);
                    }
                }
                result = string.Join(",", a);
                expected = string.Join(",", b);
            }
        }
    }
}