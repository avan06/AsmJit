using System;
using System.Runtime.InteropServices;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class AllocBlend : CompilerTestCase<Action<IntPtr, IntPtr, UIntPtr>>
    {
        protected override void Compile(CodeContext c)
        {
            //000 0f 29 74 24 d8                 movaps [rsp-0x28], xmm6
            //005 0f 29 7c 24 e8                 movaps [rsp-0x18], xmm7
            //00a 48 89 4c 24 c8                 mov [rsp-0x38], rcx
            //00f 48 89 54 24 c0                 mov [rsp-0x40], rdx
            //014 4c 89 44 24 b8                 mov [rsp-0x48], r8
            //019 48 8d 05 40 01 00 00           lea rax, [rip+0x140]
            //020 48 33 c9                       xor rcx, rcx
            //023 0f 57 c0                       xorps xmm0, xmm0
            //026 48 8b 54 24 c8                 mov rdx, [rsp-0x38]
            //02b 48 2b ca                       sub rcx, rdx
            //02e 0f 28 08                       movaps xmm1, [rax]
            //031 48 83 e1 0f                    and rcx, 0xf
            //035 0f 28 50 10                    movaps xmm2, [rax+0x10]
            //039 48 c1 e9 02                    shr rcx, 0x2
            //03d 0f 84 0d 01 00 00              jz 0x00000000150
            //043 4c 8b 44 24 b8                 mov r8, [rsp-0x48]
            //048 49 3b c8                       cmp rcx, r8
            //04b 49 0f 4f c8                    cmovg rcx, r8
            //04f 4c 2b c1                       sub r8, rcx
            //052 66 0f 74 db                    pcmpeqb xmm3, xmm3
            //056 48 8b 44 24 c0                 mov rax, [rsp-0x40]
            //05b 66 0f 6e 20                    movd xmm4, dword [rax]
            //05f 66 0f ef dc                    pxor xmm3, xmm4
            //063 66 0f 6e 2a                    movd xmm5, dword [rdx]
            //067 66 0f 71 d3 08                 psrlw xmm3, 0x8
            //06c 66 0f 60 e8                    punpcklbw xmm5, xmm0
            //070 f2 0f 70 db 55                 pshuflw xmm3, xmm3, 0x55
            //075 66 0f 60 e0                    punpcklbw xmm4, xmm0
            //079 66 0f d5 eb                    pmullw xmm5, xmm3
            //07d 66 0f ed e9                    paddsw xmm5, xmm1
            //081 66 0f e4 ea                    pmulhuw xmm5, xmm2
            //085 66 0f fd ec                    paddw xmm5, xmm4
            //089 66 0f 67 ed                    packuswb xmm5, xmm5
            //08d 66 0f 7e 2a                    movd [rdx], xmm5
            //091 48 83 c2 04                    add rdx, 0x4
            //095 48 83 c0 04                    add rax, 0x4
            //099 48 ff c9                       dec rcx
            //09c 0f 85 90 00 00 00              jnz 0x00000000132
            //0a2 4d 85 c0                       test r8, r8
            //0a5 49 8b c8                       mov rcx, r8
            //0a8 0f 84 79 00 00 00              jz 0x00000000127
            //0ae 48 83 e1 03                    and rcx, 0x3
            //0b2 49 c1 e8 02                    shr r8, 0x2
            //0b6 0f 84 62 00 00 00              jz 0x0000000011e
            //0bc 0f 10 20                       movups xmm4, [rax]
            //0bf 66 0f 74 db                    pcmpeqb xmm3, xmm3
            //0c3 0f 28 2a                       movaps xmm5, [rdx]
            //0c6 0f 57 dc                       xorps xmm3, xmm4
            //0c9 0f 28 f5                       movaps xmm6, xmm5
            //0cc 66 0f 71 d3 08                 psrlw xmm3, 0x8
            //0d1 66 0f 60 e8                    punpcklbw xmm5, xmm0
            //0d5 0f 28 fb                       movaps xmm7, xmm3
            //0d8 66 0f 61 db                    punpcklwd xmm3, xmm3
            //0dc 66 0f 68 f0                    punpckhbw xmm6, xmm0
            //0e0 66 0f 69 ff                    punpckhwd xmm7, xmm7
            //0e4 66 0f 70 db f5                 pshufd xmm3, xmm3, 0xf5
            //0e9 66 0f 70 ff f5                 pshufd xmm7, xmm7, 0xf5
            //0ee 66 0f d5 eb                    pmullw xmm5, xmm3
            //0f2 66 0f d5 f7                    pmullw xmm6, xmm7
            //0f6 66 0f ed e9                    paddsw xmm5, xmm1
            //0fa 66 0f ed f1                    paddsw xmm6, xmm1
            //0fe 66 0f e4 ea                    pmulhuw xmm5, xmm2
            //102 66 0f e4 f2                    pmulhuw xmm6, xmm2
            //106 48 83 c0 10                    add rax, 0x10
            //10a 66 0f 67 ee                    packuswb xmm5, xmm6
            //10e 66 0f fd ec                    paddw xmm5, xmm4
            //112 0f 29 2a                       movaps [rdx], xmm5
            //115 48 83 c2 10                    add rdx, 0x10
            //119 49 ff c8                       dec r8
            //11c 75 9e                          jnz 0x000000000bc
            //11e 48 85 c9                       test rcx, rcx
            //121 0f 85 1a 00 00 00              jnz 0x00000000141
            //127 0f 28 74 24 d8                 movaps xmm6, [rsp-0x28]
            //12c 0f 28 7c 24 e8                 movaps xmm7, [rsp-0x18]
            //131 c3                             ret
            //132 48 89 44 24 c0                 mov [rsp-0x40], rax
            //137 48 89 54 24 c8                 mov [rsp-0x38], rdx
            //13c e9 11 ff ff ff                 jmp 0x00000000052
            //141 48 89 44 24 c0                 mov [rsp-0x40], rax
            //146 48 89 54 24 c8                 mov [rsp-0x38], rdx
            //14b e9 02 ff ff ff                 jmp 0x00000000052
            //150 48 8b 44 24 c0                 mov rax, [rsp-0x40]
            //155 4c 8b 44 24 b8                 mov r8, [rsp-0x48]
            //15a e9 43 ff ff ff                 jmp 0x000000000a2
            //15f cc                             int3
            //160 80 00 80                       add byte [rax], 0x80
            //163 00 80 00 80 00 80              add [rax-0x7fff8000], al
            //169 00 80 00 80 00 80              add [rax-0x7fff8000], al
            //16f 00 01                          add [rcx], al
            //171 01 01                          add [rcx], eax
            //173 01 01                          add [rcx], eax
            //175 01 01                          add [rcx], eax
            //177 01 80 00 80 00 80              add [rax-0x7fff8000], eax
            //17d 00 80 00                       invalid
            var dst = c.SetArgument(c.IntPtr("dst")); //mov [rsp-0x40], rdx
            var src = c.SetArgument(c.IntPtr("src"));

            var i = c.SetArgument(c.IntPtr("i")); //mov [rsp-0x48], r8
            var j = c.IntPtr("j"); //mov [rsp-0x38], rcx
            var t = c.IntPtr("t");

            var cZero = c.Xmm("cZero");
            var cMul255A = c.Xmm("cMul255A");
            var cMul255M = c.Xmm("cMul255M");

            var x0 = c.Xmm("x0");
            var x1 = c.Xmm("x1");
            var y0 = c.Xmm("y0");
            var a0 = c.Xmm("a0");
            var a1 = c.Xmm("a1");

            var smallLoop = c.Label();
            var smallEnd = c.Label();

            var largeLoop = c.Label();
            var largeEnd = c.Label();

            var data = c.Label();

            //c.SetArgument(dst);
            //c.SetArgument(src);
            //c.SetArgument(i);

            c.Allocate(dst);
            c.Allocate(src);
            c.Allocate(i);

            // How many pixels have to be processed to make the loop aligned.
            c.Emit(InstructionId.Lea, t, Memory.Ptr(data)); //lea rax, [rip+0x140]
            c.Emit(InstructionId.Xor, j, j); //xor rcx, rcx
            c.Emit(InstructionId.Xorps, cZero, cZero); //xorps xmm0, xmm0

            c.Emit(InstructionId.Sub, j, dst); //mov rdx, [rsp-0x38]; sub rcx, rdx
            c.Emit(InstructionId.Movaps, cMul255A, Memory.Ptr(t, 0)); //movaps xmm1, [rax]

            c.Emit(InstructionId.And, j, (Immediate)15); //and rcx, 0xf
            c.Emit(InstructionId.Movaps, cMul255M, Memory.Ptr(t, 16)); //movaps xmm2, [rax+0x10]

            c.Emit(InstructionId.Shr, j, (Immediate)2); //shr rcx, 0x2
            c.Emit(InstructionId.Jz, smallEnd); //jz 0x1d3f9040150

            // j = min(i, j).
            c.Emit(InstructionId.Cmp, j, i); //mov r8, [rsp-0x48];cmp rcx, r8
            c.Emit(InstructionId.Cmovg, j, i); //cmovg rcx, r8

            // i -= j.
            c.Emit(InstructionId.Sub, i, j); //sub r8, rcx

            // Small loop.
            c.Bind(smallLoop);

            c.Emit(InstructionId.Pcmpeqb, a0, a0); //pcmpeqb xmm3, xmm3
            c.Emit(InstructionId.Movd, y0, Memory.Ptr(src)); //mov rax, [rsp-0x40];movd xmm4, dword [rax]

            c.Emit(InstructionId.Pxor, a0, y0); //pxor xmm3, xmm4
            c.Emit(InstructionId.Movd, x0, Memory.Ptr(dst)); //movd xmm5, dword [rdx]

            c.Emit(InstructionId.Psrlw, a0, (Immediate)8); //psrlw xmm3, 0x8
            c.Emit(InstructionId.Punpcklbw, x0, cZero); //punpcklbw xmm5, xmm0

            c.Emit(InstructionId.Pshuflw, a0, a0, (Immediate)Utils.Shuffle(1, 1, 1, 1)); //pshuflw xmm3, xmm3, 0x55
            c.Emit(InstructionId.Punpcklbw, y0, cZero); //punpcklbw xmm4, xmm0

            c.Emit(InstructionId.Pmullw, x0, a0); //pmullw xmm5, xmm3
            c.Emit(InstructionId.Paddsw, x0, cMul255A); //paddsw xmm5, xmm1
            c.Emit(InstructionId.Pmulhuw, x0, cMul255M); //pmulhuw xmm5, xmm2

            c.Emit(InstructionId.Paddw, x0, y0); //paddw xmm5, xmm4
            c.Emit(InstructionId.Packuswb, x0, x0); //packuswb xmm5, xmm5

            c.Emit(InstructionId.Movd, Memory.Ptr(dst), x0); //movd [rdx], xmm5

            c.Emit(InstructionId.Add, dst, (Immediate)4); //add rdx, 0x4
            c.Emit(InstructionId.Add, src, (Immediate)4); //add rax, 0x4

            c.Emit(InstructionId.Dec, j); //dec rcx
            c.Emit(InstructionId.Jnz, smallLoop); //jnz 0x000132

            // Second section, prepare for an aligned loop.
            c.Bind(smallEnd);

            c.Emit(InstructionId.Test, i, i); //test r8, r8
            c.Emit(InstructionId.Mov, j, i); //mov rcx, r8
            c.Emit(InstructionId.Jz, c.Exit); //jz 0x000127

            c.Emit(InstructionId.And, j, (Immediate)3); //and rcx, 0x3
            c.Emit(InstructionId.Shr, i, (Immediate)2); //shr r8, 0x2
            c.Emit(InstructionId.Jz, largeEnd); //jz 0x00011e

            // Aligned loop.
            c.Bind(largeLoop);

            c.Emit(InstructionId.Movups, y0, Memory.Ptr(src)); //movups xmm4, [rax]
            c.Emit(InstructionId.Pcmpeqb, a0, a0); //pcmpeqb xmm3, xmm3
            c.Emit(InstructionId.Movaps, x0, Memory.Ptr(dst)); //movaps xmm5, [rdx]

            c.Emit(InstructionId.Xorps, a0, y0); //xorps xmm3, xmm4
            c.Emit(InstructionId.Movaps, x1, x0); //movaps xmm6, xmm5

            c.Emit(InstructionId.Psrlw, a0, (Immediate)8); //psrlw xmm3, 0x8
            c.Emit(InstructionId.Punpcklbw, x0, cZero); //punpcklbw xmm5, xmm0

            c.Emit(InstructionId.Movaps, a1, a0); //movaps xmm7, xmm3
            c.Emit(InstructionId.Punpcklwd, a0, a0); //punpcklwd xmm3, xmm3

            c.Emit(InstructionId.Punpckhbw, x1, cZero); //punpckhbw xmm6, xmm0
            c.Emit(InstructionId.Punpckhwd, a1, a1); //punpckhwd xmm7, xmm7

            c.Emit(InstructionId.Pshufd, a0, a0, (Immediate)Utils.Shuffle(3, 3, 1, 1)); //pshufd xmm3, xmm3, 0xf5
            c.Emit(InstructionId.Pshufd, a1, a1, (Immediate)Utils.Shuffle(3, 3, 1, 1)); //pshufd xmm7, xmm7, 0xf5

            c.Emit(InstructionId.Pmullw, x0, a0); //pmullw xmm5, xmm3
            c.Emit(InstructionId.Pmullw, x1, a1); //pmullw xmm6, xmm7

            c.Emit(InstructionId.Paddsw, x0, cMul255A); //paddsw xmm5, xmm1
            c.Emit(InstructionId.Paddsw, x1, cMul255A); //paddsw xmm6, xmm1

            c.Emit(InstructionId.Pmulhuw, x0, cMul255M); //pmulhuw xmm5, xmm2
            c.Emit(InstructionId.Pmulhuw, x1, cMul255M); //pmulhuw xmm6, xmm2

            c.Emit(InstructionId.Add, src, (Immediate)16); //add rax, 0x10
            c.Emit(InstructionId.Packuswb, x0, x1); //packuswb xmm5, xmm6

            c.Emit(InstructionId.Paddw, x0, y0); //paddw xmm5, xmm4
            c.Emit(InstructionId.Movaps, Memory.Ptr(dst), x0); //movaps [rdx], xmm5

            c.Emit(InstructionId.Add, dst, (Immediate)16); //add rdx, 0x10

            c.Emit(InstructionId.Dec, i); //dec r8
            c.Emit(InstructionId.Jnz, largeLoop); //jnz 0x0000bc

            c.Bind(largeEnd);
            c.Emit(InstructionId.Test, j, j); //test rcx, rcx
            c.Emit(InstructionId.Jnz, smallLoop); //jnz 0x000141

            // Data
            c.Data(data, 16, 
                Data.Of(0x0080008000800080, 0x0080008000800080),
                Data.Of(0x0101010101010101, 0x0080008000800080));
        }

        protected override unsafe void Execute(Action<IntPtr, IntPtr, UIntPtr> fn, out string result, out string expected)
        {
            const int cnt = 17;

            #region InitTestArguments
            var dst = new uint[] { 0x00000000, 0x10101010, 0x20100804, 0x30200003, 0x40204040, 0x5000004D, 0x60302E2C, 0x706F6E6D, 0x807F4F2F, 0x90349001, 0xA0010203, 0xB03204AB, 0xC023AFBD, 0xD0D0D0C0, 0xE0AABBCC, 0xFFFFFFFF, 0xF8F4F2F1 };
            var src = new uint[] { 0xE0E0E0E0, 0xA0008080, 0x341F1E1A, 0xFEFEFEFE, 0x80302010, 0x49490A0B, 0x998F7798, 0x00000000, 0x01010101, 0xA0264733, 0xBAB0B1B9, 0xFF000000, 0xDAB0A0C1, 0xE0BACFDA, 0x99887766, 0xFFFFFF80, 0xEE0A5FEC };

            var rawptr = Marshal.AllocHGlobal((cnt + 3) * sizeof(uint) + 8);
            var pdst = new IntPtr(16 * (((long)rawptr + 15) / 16));

            rawptr = Marshal.AllocHGlobal((cnt + 3) * sizeof(uint) + 8);
            var psrc = new IntPtr(16 * (((long)rawptr + 15) / 16));

            UnsafeMemory.Copy(pdst, Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), cnt * sizeof(uint));
            UnsafeMemory.Copy(psrc, Marshal.UnsafeAddrOfPinnedArrayElement(src, 0), cnt * sizeof(uint));
            #endregion

            var e = new uint[cnt];
            for (var z = 0; z < cnt; z++)
            {
                e[z] = BlendSrcOver(dst[z], src[z]);				
            }

            fn(pdst, psrc, (UIntPtr)cnt);

            UnsafeMemory.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), pdst, cnt*sizeof(uint));

            result = string.Join(",", dst);
            expected = string.Join(",", e);
        }

        private static uint BlendSrcOver(uint d, uint s)
        {
            var saInv = ~s >> 24;

            var d20 = (d) & 0x00FF00FF;
            var d31 = (d >> 8) & 0x00FF00FF;

            d20 *= saInv;
            d31 *= saInv;

            d20 = ((d20 + ((d20 >> 8) & 0x00FF00FFU) + 0x00800080U) & 0xFF00FF00U) >> 8;
            d31 = ((d31 + ((d31 >> 8) & 0x00FF00FFU) + 0x00800080U) & 0xFF00FF00U);

            return d20 + d31 + s;
        }
    }
}