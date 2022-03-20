using System;
using System.Runtime.ExceptionServices;
using AsmJit.AssemblerContext;

namespace AsmJitTest.TestCases
{
    public sealed class RawAssembler2 : AssemblerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
//            c.Emit(@"
//mov al, [eax+ebx+0x80]
//mov al, [eax+ebx]
//mov al, [eax+0x80]
//mov al, [0x13d3000000040027]
//");
            c.Emit(@"
mov eax, 200
mov ecx, 33
add eax, ecx
ret
");
        }

        [HandleProcessCorruptedStateExceptions]

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = "";
            expected = (200 + 33).ToString();
            try { result = fn().ToString(); } catch { }
        }
    }
}