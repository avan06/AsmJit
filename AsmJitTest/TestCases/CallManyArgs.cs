using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallManyArgs : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var fn = c.IntPtr("fn");
            var va = c.Int32("va");
            var vb = c.Int32("vb");
            var vc = c.Int32("vc");
            var vd = c.Int32("vd");
            var ve = c.Int32("ve");
            var vf = c.Int32("vf");
            var vg = c.Int32("vg");
            var vh = c.Int32("vh");
            var vi = c.Int32("vi");
            var vj = c.Int32("vj");

            var fp = FnPointer.Fn(new Func<int, int, int, int, int, int, int, int, int, int, int>(CalledFunction));

            c.Emit(
                Inst.Mov, fn, fp,
                Inst.Mov, va, (Immediate)0x03,
                Inst.Mov, vb, (Immediate)0x12,
                Inst.Mov, vc, (Immediate)0xA0,
                Inst.Mov, vd, (Immediate)0x0B,
                Inst.Mov, ve, (Immediate)0x2F,
                Inst.Mov, vf, (Immediate)0x02,
                Inst.Mov, vg, (Immediate)0x0C,
                Inst.Mov, vh, (Immediate)0x12,
                Inst.Mov, vi, (Immediate)0x18,
                Inst.Mov, vj, (Immediate)0x1E);

            var call = c.Call(fn, fp);
            call.SetArgument(va, vb, vc, vd, ve, vf, vg, vh, vi, vj);
            call.SetReturn(va);

            c.Ret(va);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = CalledFunction(0x03, 0x12, 0xA0, 0x0B, 0x2F, 0x02, 0x0C, 0x12, 0x18, 0x1E).ToString();
        }

        private static int CalledFunction(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
        {
            return (a * b * c * d * e) + (f * g * h * i * j);
        }
    }
}