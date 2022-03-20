using System;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocManual : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
//00000141de880000 33 c0                          xor eax, eax
//00000141de880002 33 c9                          xor ecx, ecx
//00000141de880004 89 44 24 f0                    mov [rsp-0x10], eax
//00000141de880008 89 4c 24 ec                    mov [rsp-0x14], ecx
//00000141de88000c b8 20 00 00 00                 mov eax, 0x20
//00000141de880011 8b 4c 24 ec                    mov ecx, [rsp-0x14]
//00000141de880015 ff c1                          inc ecx
//00000141de880017 8b 54 24 f0                    mov edx, [rsp-0x10]
//00000141de88001b 03 d1                          add edx, ecx
//00000141de88001d ff c8                          dec eax
//00000141de88001f 0f 85 07 00 00 00              jnz 0x141de88002c
//00000141de880025 89 44 24 e8                    mov [rsp-0x18], eax
//00000141de880029 8b c2                          mov eax, edx
//00000141de88002b c3                             ret
//00000141de88002c 89 4c 24 ec                    mov [rsp-0x14], ecx
//00000141de880030 89 54 24 f0                    mov [rsp-0x10], edx
//00000141de880034 eb db                          jmp 0x141de880011
            var v0 = c.Int32("v0");
            var v1 = c.Int32("v1");
            var cnt = c.Int32("cnt");

            c.Emit(InstructionId.Xor, v0, v0,
                InstructionId.Xor, v1, v1);
            c.Spill(v0);
            c.Spill(v1);

            var l = c.Label();
            c.Emit(InstructionId.Mov, cnt, (Immediate)32);

            c.Bind(l);
            c.Emit(
                InstructionId.Inc, v1,
                InstructionId.Add, v0, v1,

                InstructionId.Dec, cnt,
                InstructionId.Jnz, l);

            c.Ret(v0);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = (0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32).ToString();
        }
    }
}