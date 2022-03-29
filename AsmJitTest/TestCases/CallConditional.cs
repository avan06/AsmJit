using System;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJitTest.TestCases
{
    public sealed class CallConditional : CompilerTestCase<Func<int, int, int, int>>
    {
//		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//		delegate int CalledFunctionDel(int x, int y);

        protected override void Compile(CodeContext c)
        {
            //00000186b0360000 53                             push rbx
            //00000186b0360001 48 83 ec 30                    sub rsp, 0x30
            //00000186b0360005 41 83 f8 00                    cmp r8d, 0x0
            //00000186b0360009 0f 84 4f 00 00 00              jz 0x186b036005e
            //00000186b036000f 41 83 f8 01                    cmp r8d, 0x1
            //00000186b0360013 0f 84 4b 00 00 00              jz 0x186b0360064
            //00000186b0360019 b8 00 00 00 00                 mov eax, 0x0
            //00000186b036001e e9 35 00 00 00                 jmp 0x186b0360058
            //00000186b0360023 48 bb 7c 0e 38 b0 86 01 00 00  mov rbx, 0x186b0380e7c
            //00000186b036002d 89 44 24 2c                    mov [rsp+0x2c], eax
            //00000186b0360031 89 4c 24 28                    mov [rsp+0x28], ecx
            //00000186b0360035 89 54 24 24                    mov [rsp+0x24], edx
            //00000186b0360039 ff d3                          call rbx
            //00000186b036003b e9 18 00 00 00                 jmp 0x186b0360058
            //00000186b0360040 48 bb cc 0e 38 b0 86 01 00 00  mov rbx, 0x186b0380ecc
            //00000186b036004a 8b 4c 24 28                    mov ecx, [rsp+0x28]
            //00000186b036004e 8b 54 24 24                    mov edx, [rsp+0x24]
            //00000186b0360052 89 44 24 20                    mov [rsp+0x20], eax
            //00000186b0360056 ff d3                          call rbx
            //00000186b0360058 48 83 c4 30                    add rsp, 0x30
            //00000186b036005c 5b                             pop rbx
            //00000186b036005d c3                             ret
            //00000186b036005e 8b 44 24 2c                    mov eax, [rsp+0x2c]
            //00000186b0360062 eb bf                          jmp 0x186b0360023
            //00000186b0360064 8b 44 24 20                    mov eax, [rsp+0x20]
            //00000186b0360068 89 4c 24 28                    mov [rsp+0x28], ecx
            //00000186b036006c 89 54 24 24                    mov [rsp+0x24], edx
            //00000186b0360070 eb ce                          jmp 0x186b0360040
            var x = c.SetArgument(c.Int32("x"));
            var y = c.SetArgument(c.Int32("y"));
            var op = c.SetArgument(c.Int32("op"));

            var opAdd = c.Label();
            var opMul = c.Label();

            c.Emit(Inst.Cmp, op, (Immediate)0);
            c.Emit(Inst.Jz, opAdd);
            c.Emit(Inst.Cmp, op, (Immediate)1);
            c.Emit(Inst.Jz, opMul);

            var result = c.Int32("result_0");
            c.Emit(Inst.Mov, result, (Immediate)0);
            c.Ret(result);

            c.Bind(opAdd);
            var call = c.Call(FnPointer.Fn(new Func<int, int, int>(CalledFunctionAdd)));
            call.SetArgument(x, y);
            var result_1 = call.SetReturn(c.Int32("result_1"));
            c.Ret(result_1);

            c.Bind(opMul);
            call = c.Call(FnPointer.Fn(new Func<int, int, int>(CalledFunctionMul)));
            call.SetArgument(x, y);
            var result_2 = call.SetReturn(c.Int32("result_2"));
            c.Ret(result_2);
        }

        protected override void Execute(Func<int, int, int, int> fn, out string result, out string expected)
        {
            var add = fn(4, 8, 0);
            var mul = fn(4, 8, 1);
            result = add + " " + mul;
            expected = CalledFunctionAdd(4, 8) + " " + CalledFunctionMul(4, 8);
        }

        private static int CalledFunctionAdd(int x, int y) => x + y;

        private static int CalledFunctionMul(int x, int y) => x * y;
    }
}