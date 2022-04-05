using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocManual : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
            //141de880000 33 c0              xor eax, eax
            //141de880002 33 c9              xor ecx, ecx
            //141de880004 89 44 24 f0        mov [rsp-0x10], eax
            //141de880008 89 4c 24 ec        mov [rsp-0x14], ecx
            //141de88000c b8 20 00 00 00     mov eax, 0x20
            //141de880011 8b 4c 24 ec        mov ecx, [rsp-0x14]
            //141de880015 ff c1              inc ecx
            //141de880017 8b 54 24 f0        mov edx, [rsp-0x10]
            //141de88001b 03 d1              add edx, ecx
            //141de88001d ff c8              dec eax
            //141de88001f 0f 85 07 00 00 00  jnz 0x141de88002c
            //141de880025 89 44 24 e8        mov [rsp-0x18], eax
            //141de880029 8b c2              mov eax, edx
            //141de88002b c3                 ret
            //141de88002c 89 4c 24 ec        mov [rsp-0x14], ecx
            //141de880030 89 54 24 f0        mov [rsp-0x10], edx
            //141de880034 eb db              jmp 0x141de880011
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");
            var cnt = c.Int32("cnt");

            c.Emit(
                Inst.Xor, v0, v0,
                Inst.Xor, v1, v1);
            c.Spill(v0, v1);

            var l = c.Label();
            c.Emit(Inst.Mov, cnt, (Immediate)32);

            c.Bind(l);
            c.Emit(
                Inst.Inc, v1,
                Inst.Add, v0, v1,

                Inst.Dec, cnt,
                Inst.Jnz, l);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32).ToString();
        }
    }
}