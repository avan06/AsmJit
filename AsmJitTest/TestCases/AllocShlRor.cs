using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocShlRor : CompilerTestCase<Action<IntPtr, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var dst = c.SetArgument(c.IntPtr("dst"));
            var var = c.SetArgument(c.Int32("var"));
            var vShlParam = c.SetArgument(c.Int32("vShlParam"));
            var vRorParam = c.SetArgument(c.Int32("vRorParam"));

            c.Emit(InstructionId.Shl, var, vShlParam);
            c.Emit(InstructionId.Ror, var, vRorParam);

            c.Emit(InstructionId.Mov, Memory.DWord(dst), var);
        }

        protected override void Execute(Action<IntPtr, int, int, int> fn, out string result, out string expected)
        {
            var v0 = 0x000000FF;
            int resultRet;
            unsafe
            {				
                fn((IntPtr)(&resultRet), v0, 16, 8);
            }
            result = resultRet.ToString();
            expected = 0x0000FF00.ToString();
        }
    }
}