using System;
using System.Runtime.ExceptionServices;
using AsmJit.AssemblerContext;

namespace AsmJitTest.TestCases
{
    public sealed class RawAssembler3 : AssemblerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            //var R = Cpu.Registers;
            //c.Emit(Inst.Mov, R.Al, Memory.QWord(R.Eax, R.Ebx, 0, 0x80));
            //c.Emit(Inst.Lea, R.Rcx, Memory.QWord(R.Eax, R.Rip, 0x2a8c)); //lea   rcx,[rip+0x2a8c] 
            //c.Emit(Inst.Mov, R.Al, Memory.QWord(R.Eax, 0x80));
            //c.Emit(Inst.Mov, R.Al, Memory.QWordAbs((IntPtr)(0x13d3000000040027 & 0xFFFFFFFFu), R.Eax));

            //            c.Emit(@"
            //mov al, [eax+ebx+0x80]
            //mov al, [eax+ebx]
            //mov al, [eax+0x80]
            //mov al, [0x13d3000000040027]
            //");
//mov eax, 200
//mov ecx, 33
//Xchg eax, ecx
//Xchg eax, [ecx+10]
//Xchg [eax+10], ecx
//Fcom [eax+10]
//Fld eax
//Fld [eax+10]
//Faddp
//            c.Emit(@"
//mov dword [rbp-0xc], 0x1
//add dword [rbp-0xc], 0x10
//mov edx, [rbp-0xc]
//mov eax, [rbp+rax*4-0x40]
//mov eax, edx
//shl eax, 0x2
//add eax, edx
//add eax, eax
//movsxd rdx, eax
//ret
//");
            c.Emit(@"
mov dword [rbp-0xc], 0x1
add word [rbp-0xc], 0x10
mov eax, [rax*4+0x40]
mov eax, [fs:rax]
mov eax, [es:rbp]
mov eax, [rbp+rax*4]
ret
");
        }

        [HandleProcessCorruptedStateExceptions]

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = "";
            expected = "";
            try { result = fn().ToString(); } catch { }
        }
    }
}