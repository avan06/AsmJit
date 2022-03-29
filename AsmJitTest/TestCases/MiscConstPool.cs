using System;
using AsmJit.Common;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class MiscConstPool : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");

            var c0 = c.Int32Const(ConstantScope.Local, 246);
            var c1 = c.Int32Const(ConstantScope.Local, 135);

            c.Emit(
                Inst.Mov, v0, c0,
                Inst.Mov, v1, c1,
                Inst.Add, v0, v1);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (246+135).ToString();
        }
    }
}