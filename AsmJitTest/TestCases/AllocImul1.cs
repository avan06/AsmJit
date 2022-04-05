using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocImul1 : CompilerTestCase<Action<IntPtr, IntPtr, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            //0000 48 89 54 24 e8  mov [rsp-0x18], rdx
            //0005 41 8b c0        mov eax, r8d
            //0008 41 f7 e9        imul r9d
            //000b 89 11           mov [rcx], edx
            //000d 48 8b 54 24 e8  mov rdx, [rsp-0x18]
            //0012 89 02           mov [rdx], eax
            //0014 c3              rets
            var dstHi = c.IntPtr("dstHi");
            var dstLo = c.IntPtr("dstLo");
            var vLo = c.Int32("vLo");
            var src = c.Int32("src");

            var vHi = c.Int32("vHi");

            c.SetArgument(dstHi, dstLo, vLo, src);
            c.Emit(
                Inst.Imul, vHi, vLo, src, //vHi = vLo * src

                Inst.Mov, Memory.DWord(dstHi), vHi,
                Inst.Mov, Memory.DWord(dstLo), vLo);
        }

        protected override void Execute(Action<IntPtr, IntPtr, int, int> fn, out string result, out string expected)
        {
            var v0 = 9999;
            var v1 = 8888;
            int resultHi;
            int resultLo;
            unsafe
            {
                fn((IntPtr)(&resultHi), (IntPtr)(&resultLo), v0, v1);
            }
            result = resultHi + " " + resultLo;
            expected = string.Format("{0} {1}", ((long)v0 * v1) >> 32, ((long)v0 * v1) & 0xFFFFFFFF);
        }
    }
}