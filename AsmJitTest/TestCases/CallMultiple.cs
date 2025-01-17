using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallMultiple : CompilerTestCase<Func<IntPtr, int>>
    {
        protected override void Compile(CodeContext c)
        {
            int i;

            var buf = c.SetArgument(c.IntPtr("buf"));
            var acc0 = c.Int32("acc0");
            var acc1 = c.Int32("acc1");

            c.Emit(Inst.Mov, acc0, (Immediate)0);
            c.Emit(Inst.Mov, acc1, (Immediate)0);

            for (i = 0; i < 4; i++)
            {
                var ret = c.Int32("ret");
                var ptr = c.IntPtr("ptr");
                var idx = c.Int32("idx");

                c.Emit(
                    Inst.Mov, ptr, buf,
                    Inst.Mov, idx, (Immediate)i);

                var call = c.Call(FnPointer.Fn(new Func<IntPtr, int, int>(CalledFunction)/*, CallingConvention.HostDefaultFast*/));
                call.SetArgument(0, ptr);
                call.SetArgument(1, idx);
                call.SetReturn(0, ret);

                c.Emit(
                    Inst.Add, acc0, ret,

                    Inst.Mov, ptr, buf,
                    Inst.Mov, idx, (Immediate)i);

                call = c.Call(FnPointer.Fn(new Func<IntPtr, int, int>(CalledFunction)/*, CallingConvention.HostDefaultFast*/));
                call.SetArgument(0, ptr);
                call.SetArgument(1, idx);
                call.SetReturn(0, ret);

                c.Emit(Inst.Sub, acc1, ret);
            }

            c.Emit(Inst.Add, acc0, acc1);
            c.Ret(acc0);
        }

        protected override void Execute(Func<IntPtr, int> fn, out string result, out string expected)
        {
            var ar = new[] { 127, 87, 23, 17 };
            unsafe
            {
                fixed (int* buffer = ar)
                {
                    result = fn((IntPtr)buffer).ToString();
                }
            }
            expected = 0.ToString();
        }

        private static unsafe int CalledFunction(IntPtr pInt, int index) => ((int*)pInt)[index];
    }
}