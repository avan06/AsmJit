using System;
using AsmJit.Common;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocUseMem : CompilerTestCase<Func<int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            //15df0db0000 8b c1         mov eax, ecx
            //15df0db0002 8b ca         mov ecx, edx
            //15df0db0004 89 4c 24 e8   mov [rsp-0x18], ecx
            //15df0db0008 ff c0         inc eax
            //15df0db000a 3b 44 24 e8   cmp eax, [rsp-0x18]
            //15df0db000e 75 f8         jnz 0x15df0db0008
            //15df0db0010 c3            ret
            var l1 = c.Label();

            var iIdx = c.Int32("iIdx");
            var iEnd = c.Int32("iEnd");

            var aIdx = c.Int32("aIdx");
            var aEnd = c.Int32("aEnd");

            c.SetArgument(aIdx, aEnd);
            c.Emit(
                Inst.Mov, iIdx, aIdx,
                Inst.Mov, iEnd, aEnd);
            c.Spill(iEnd);

            c.Bind(l1);
            c.Emit(
                Inst.Inc, iIdx,
                Inst.Cmp, iIdx, iEnd.ToMemory(),
                Inst.Jne, l1);

            c.Ret(iIdx);
        }

        protected override void Execute(Func<int, int, int> fn, out string result, out string expected)
        {
            result = fn(10, 20).ToString();
            expected = 20.ToString();
        }
    }
}