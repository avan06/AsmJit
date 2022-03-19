using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocSetz : CompilerTestCase<Action<int, int, IntPtr>>
    {
        protected override void Compile(CodeContext c)
        {
            var src0 = c.SetArgument(c.Int32("src0"));
            var src1 = c.SetArgument(c.Int32("src1"));
            var dst0 = c.SetArgument(c.IntPtr("dst0"));

            c.Emit(InstructionId.Cmp, src0, src1);
            c.Emit(InstructionId.Setz, Memory.Byte(dst0));
        }

        protected override void Execute(Action<int, int, IntPtr> fn, out string result, out string expected)
        {
            var resultBuf = new byte[4];
            var expectedBuf = new byte[] {1, 0, 0, 1};

            unsafe
            {
                fixed (byte* rb = resultBuf)
                {
                    fn(0, 0, (IntPtr) (&rb[0]));
                    fn(0, 1, (IntPtr) (&rb[1]));
                    fn(1, 0, (IntPtr) (&rb[2]));
                    fn(1, 1, (IntPtr) (&rb[3]));
                }
            }

            result = string.Join(",", resultBuf);
            expected = string.Join(",", expectedBuf);
        }
    }
}