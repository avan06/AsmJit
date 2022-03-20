using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocStack : CompilerTestCase<Func<int>>
    {
        protected override void Compile(CodeContext c)
        {
//0000021a77680000 48 81 ec 18 01 00 00           sub rsp, 0x118
//0000021a77680007 48 33 c0                       xor rax, rax
//0000021a7768000a 88 44 04 08                    mov [rsp+rax+0x8], al
//0000021a7768000e 48 ff c0                       inc rax
//0000021a77680011 48 3d ff 00 00 00              cmp rax, 0xff
//0000021a77680017 7e f1                          jle 0x21a7768000a
//0000021a77680019 48 33 c0                       xor rax, rax
//0000021a7768001c 33 c9                          xor ecx, ecx
//0000021a7768001e 0f b6 54 04 08                 movzx edx, byte [rsp+rax+0x8]
//0000021a77680023 03 ca                          add ecx, edx
//0000021a77680025 48 ff c0                       inc rax
//0000021a77680028 48 3d ff 00 00 00              cmp rax, 0xff
//0000021a7768002e 7e ee                          jle 0x21a7768001e
//0000021a77680030 48 89 04 24                    mov [rsp], rax
//0000021a77680034 8b c1                          mov eax, ecx
//0000021a77680036 48 81 c4 18 01 00 00           add rsp, 0x118
//0000021a7768003d c3                             ret
            var stack = c.Stack(256, 1).SetSize(1);
            var i = c.IntPtr("i");
            var a = c.Int32("a");
            var b = c.Int32("b");

            var l1 = c.Label();
            var l2 = c.Label();

            // Fill stack by sequence [0, 1, 2, 3 ... 255].
            c.Emit(InstructionId.Xor, i, i);

            c.Bind(l1);
            c.Emit(
                InstructionId.Mov, stack.Clone().SetIndex(i), i.As8(),
                InstructionId.Inc, i,
                InstructionId.Cmp, i, (Immediate)255,
                InstructionId.Jle, l1);

            // Sum sequence in stack.
            c.Emit(
                InstructionId.Xor, i, i,
                InstructionId.Xor, a, a);

            c.Bind(l2);
            c.Emit(
                InstructionId.Movzx, b, stack.Clone().SetIndex(i),
                InstructionId.Add, a, b,
                InstructionId.Inc, i,
                InstructionId.Cmp, i, (Immediate)255,
                InstructionId.Jle, l2);

            c.Ret(a);
        }

        protected override void Execute(Func<int> fn, out string result, out string expected)
        {
            result = fn().ToString();
            expected = 32640.ToString();
        }
    }
}