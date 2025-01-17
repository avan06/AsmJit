using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class MiscMultiRet : CompilerTestCase<Func<int, int, int, int>>
    {
        protected override void Compile(CodeContext c)
        {
            var op = c.Int32("op");
            var a = c.Int32("a");
            var b = c.Int32("b");

            var zero = c.Label();
            var add = c.Label();
            var sub = c.Label();
            var mul = c.Label();
            var div = c.Label();

            c.SetArgument(0, op);
            c.SetArgument(1, a);
            c.SetArgument(2, b);

            c.Emit(
                Inst.Cmp, op, (Immediate)0,
                Inst.Jz, add,

                Inst.Cmp, op, (Immediate)1,
                Inst.Jz, sub,

                Inst.Cmp, op, (Immediate)2,
                Inst.Jz, mul,

                Inst.Cmp, op, (Immediate)3,
                Inst.Jz, div);

            c.Bind(zero);
            c.Emit(Inst.Xor, a, a);
            c.Ret(a);

            c.Bind(add);
            c.Emit(Inst.Add, a, b);
            c.Ret(a);

            c.Bind(sub);
            c.Emit(Inst.Sub, a, b);
            c.Ret(a);

            c.Bind(mul);
            c.Emit(Inst.Imul, a, b);
            c.Ret(a);

            c.Bind(div);
            c.Emit(
                Inst.Cmp, b, (Immediate)0,
                Inst.Jz, zero);

            var z = c.Int32("z");
            c.Emit(
                Inst.Xor, z, z,
                Inst.Idiv, z, a, b);
            c.Ret(a);
        }

        protected override void Execute(Func<int, int, int, int> fn, out string result, out string expected)
        {
            var a = 44;
            var b = 3;

            var r0 = fn(0, a, b);
            var r1 = fn(1, a, b);
            var r2 = fn(2, a, b);
            var r3 = fn(3, a, b);

            var e0 = a + b;
            var e1 = a - b;
            var e2 = a * b;
            var e3 = a / b;

            result = string.Format("{0} {1} {2} {3}", r0, r1, r2, r3);
            expected = string.Format("{0} {1} {2} {3}", e0, e1, e2, e3);
        }
    }
}