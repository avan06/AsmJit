using System;
using AsmJit.Common;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocMemcpy : CompilerTestCase<Action<IntPtr, IntPtr, UIntPtr>>
    {
        protected override void Compile(CodeContext c)
        {
//0000021a77690000 48 89 4c 24 e8                 mov [rsp-0x18], rcx
//0000021a77690005 48 89 54 24 e0                 mov [rsp-0x20], rdx
//0000021a7769000a 4c 89 44 24 d8                 mov [rsp-0x28], r8
//0000021a7769000f 4c 8b 44 24 d8                 mov r8, [rsp-0x28]
//0000021a77690014 4d 85 c0                       test r8, r8
//0000021a77690017 0f 84 31 00 00 00              jz 0x21a7769004e
//0000021a7769001d 48 8b 54 24 e0                 mov rdx, [rsp-0x20]
//0000021a77690022 8b 02                          mov eax, [rdx]
//0000021a77690024 48 8b 4c 24 e8                 mov rcx, [rsp-0x18]
//0000021a77690029 89 01                          mov [rcx], eax
//0000021a7769002b 48 83 c2 04                    add rdx, 0x4
//0000021a7769002f 48 83 c1 04                    add rcx, 0x4
//0000021a77690033 49 ff c8                       dec r8
//0000021a77690036 0f 85 01 00 00 00              jnz 0x21a7769003d
//0000021a7769003c c3                             ret
//0000021a7769003d 48 89 4c 24 e8                 mov [rsp-0x18], rcx
//0000021a77690042 48 89 54 24 e0                 mov [rsp-0x20], rdx
//0000021a77690047 4c 89 44 24 d8                 mov [rsp-0x28], r8
//0000021a7769004c eb cf                          jmp 0x21a7769001d
//0000021a7769004e 48 8b 4c 24 e8                 mov rcx, [rsp-0x18]
//0000021a77690053 48 8b 54 24 e0                 mov rdx, [rsp-0x20]
//0000021a77690058 eb e2                          jmp 0x21a7769003c
            var dst = c.IntPtr("dst");
            var src = c.IntPtr("src");
            var cnt = c.UIntPtr("cnt");

            var loop = c.Label(); // Create base labels we use
            var exit = c.Label(); // in our function.

            c.SetArgument(dst, src, cnt);
            c.Allocate(dst, src, cnt); // Allocate all registers now, because we want to keep them in physical registers only.

            c.Emit(
                Inst.Test, cnt, cnt, // Exit if length is zero.
                Inst.Jz, exit);

            c.Bind(loop); // Bind the loop label here.

            var tmp = c.Int32("tmp"); // Copy a single dword (4 bytes).
            c.Emit(Inst.Mov, tmp, Memory.DWord(src),
                Inst.Mov, Memory.DWord(dst), tmp,

                Inst.Add, src, (Immediate)4, // Increment dst/src pointers.
                Inst.Add, dst, (Immediate)4,

                Inst.Dec, cnt, // Loop until cnt isn't zero.
                Inst.Jnz, loop);

            c.Bind(exit); // Bind the exit label here.
        }

        protected override void Execute(Action<IntPtr, IntPtr, UIntPtr> fn, out string result, out string expected)
        {
            unsafe
            {
                const int cnt = 32;
                var a = new int[cnt].InitializeWith(i => i);
                var b = new int[cnt];

                fixed (int* src = a)
                {
                    fixed (int* dst = b)
                    {
                        fn((IntPtr)dst, (IntPtr)src, (UIntPtr)cnt);
                    }
                }
                result = string.Join(",", a);
                expected = string.Join(",", b);
            }
        }
    }
}