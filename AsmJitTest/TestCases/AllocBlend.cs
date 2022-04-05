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
            //000 0f 29 74 24 d8        movaps [rsp-0x28], xmm6
            //005 0f 29 7c 24 e8        movaps [rsp-0x18], xmm7
            //00a 48 89 4c 24 c8        mov [rsp-0x38], rcx
            //00f 48 89 54 24 c0        mov [rsp-0x40], rdx
            //014 4c 89 44 24 b8        mov [rsp-0x48], r8
            //019 48 8d 05 40 01 00 00  lea rax, [rip+0x140]
            //020 0f 28 00              movaps xmm0, [rax]
            //023 0f 28 48 10           movaps xmm1, [rax+0x10]
            //027 48 33 c0              xor rax, rax
            //02a 0f 57 d2              xorps xmm2, xmm2
            //02d 48 8b 4c 24 c8        mov rcx, [rsp-0x38]
            //032 48 2b c1              sub rax, rcx
            //035 48 83 e0 0f           and rax, 0xf
            //039 48 c1 e8 02           shr rax, 0x2
            //03d 0f 84 0d 01 00 00     jz 0x1f698060150
            //043 4c 8b 44 24 b8        mov r8, [rsp-0x48]
            //048 49 3b c0              cmp rax, r8
            //04b 49 0f 4f c0           cmovg rax, r8
            //04f 4c 2b c0              sub r8, rax
            //052 48 8b 54 24 c0        mov rdx, [rsp-0x40]
            //057 66 0f 6e 1a           movd xmm3, dword [rdx]
            //05b 66 0f 6e 21           movd xmm4, dword [rcx]
            //05f 66 0f 74 ed           pcmpeqb xmm5, xmm5
            //063 66 0f ef eb           pxor xmm5, xmm3
            //067 66 0f 71 d5 08        psrlw xmm5, 0x8
            //06c 66 0f 60 e2           punpcklbw xmm4, xmm2
            //070 f2 0f 70 ed 55        pshuflw xmm5, xmm5, 0x55
            //075 66 0f 60 da           punpcklbw xmm3, xmm2
            //079 66 0f d5 e5           pmullw xmm4, xmm5
            //07d 66 0f ed e0           paddsw xmm4, xmm0
            //081 66 0f e4 e1           pmulhuw xmm4, xmm1
            //085 66 0f fd e3           paddw xmm4, xmm3
            //089 66 0f 67 e4           packuswb xmm4, xmm4
            //08d 66 0f 7e 21           movd [rcx], xmm4
            //091 48 83 c1 04           add rcx, 0x4
            //095 48 83 c2 04           add rdx, 0x4
            //099 48 ff c8              dec rax
            //09c 0f 85 90 00 00 00     jnz 0x132
            //0a2 4d 85 c0              test r8, r8
            //0a5 49 8b c0              mov rax, r8
            //0a8 0f 84 79 00 00 00     jz 0x127
            //0ae 48 83 e0 03           and rax, 0x3
            //0b2 49 c1 e8 02           shr r8, 0x2
            //0b6 0f 84 62 00 00 00     jz 0x11e
            //0bc 0f 10 1a              movups xmm3, [rdx]
            //0bf 0f 28 21              movaps xmm4, [rcx]
            //0c2 66 0f 74 ed           pcmpeqb xmm5, xmm5
            //0c6 0f 57 eb              xorps xmm5, xmm3
            //0c9 0f 28 f4              movaps xmm6, xmm4
            //0cc 66 0f 71 d5 08        psrlw xmm5, 0x8
            //0d1 66 0f 60 e2           punpcklbw xmm4, xmm2
            //0d5 0f 28 fd              movaps xmm7, xmm5
            //0d8 66 0f 61 ed           punpcklwd xmm5, xmm5
            //0dc 66 0f 68 f2           punpckhbw xmm6, xmm2
            //0e0 66 0f 69 ff           punpckhwd xmm7, xmm7
            //0e4 66 0f 70 ed f5        pshufd xmm5, xmm5, 0xf5
            //0e9 66 0f 70 ff f5        pshufd xmm7, xmm7, 0xf5
            //0ee 66 0f d5 e5           pmullw xmm4, xmm5
            //0f2 66 0f d5 f7           pmullw xmm6, xmm7
            //0f6 66 0f ed e0           paddsw xmm4, xmm0
            //0fa 66 0f ed f0           paddsw xmm6, xmm0
            //0fe 66 0f e4 e1           pmulhuw xmm4, xmm1
            //102 66 0f e4 f1           pmulhuw xmm6, xmm1
            //106 48 83 c2 10           add rdx, 0x10
            //10a 66 0f 67 e6           packuswb xmm4, xmm6
            //10e 66 0f fd e3           paddw xmm4, xmm3
            //112 0f 29 21              movaps [rcx], xmm4
            //115 48 83 c1 10           add rcx, 0x10
            //119 49 ff c8              dec r8
            //11c 75 9e                 jnz 0x0bc
            //11e 48 85 c0              test rax, rax
            //121 0f 85 1a 00 00 00     jnz 0x141
            //127 0f 28 74 24 d8        movaps xmm6, [rsp-0x28]
            //12c 0f 28 7c 24 e8        movaps xmm7, [rsp-0x18]
            //131 c3                    ret
            //132 48 89 54 24 c0        mov [rsp-0x40], rdx
            //137 48 89 4c 24 c8        mov [rsp-0x38], rcx
            //13c e9 11 ff ff ff        jmp 0x052
            //141 48 89 54 24 c0        mov [rsp-0x40], rdx
            //146 48 89 4c 24 c8        mov [rsp-0x38], rcx
            //14b e9 02 ff ff ff        jmp 0x052
            //150 48 8b 54 24 c0        mov rdx, [rsp-0x40]
            //155 4c 8b 44 24 b8        mov r8, [rsp-0x48]
            //15a e9 43 ff ff ff        jmp 0x0a2
            //15f cc                    int3
            //160 80 00 80              add byte [rax], 0x80
            //163 00 80 00 80 00 80     add [rax-0x7fff8000], al
            //169 00 80 00 80 00 80     add [rax-0x7fff8000], al
            //16f 00 01                 add [rcx], al
            //171 01 01                 add [rcx], eax
            //173 01 01                 add [rcx], eax
            //175 01 01                 add [rcx], eax
            //177 01 01                 add [rcx], eax
            //179 01 01                 add [rcx], eax
            //17b 01 01                 add [rcx], eax
            //17d 01 01                 add [rcx], eax
            //17f 01                    invalid
            var dst = c.IntPtr("dst");
            var src = c.IntPtr("src");
            var idx = c.IntPtr("idx");

            var j = c.IntPtr("j");
            var t = c.IntPtr("t");

            var cZero = c.Xmm("cZero");
            var cMul255A_0080 = c.Xmm("cMul255A");
            var cMul255M_0101 = c.Xmm("cMul255M");

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

            c.SetArgument(dst, src, idx);
            c.Allocate(dst, src, idx);

            // How many pixels have to be processed to make the loop aligned.
            c.Emit(
                Inst.Lea, t, Memory.Ptr(data),
                Inst.Movaps, cMul255A_0080, Memory.Ptr(t, 0),
                Inst.Movaps, cMul255M_0101, Memory.Ptr(t, 16),

                Inst.Xor, j, j,
                Inst.Xorps, cZero, cZero,

                Inst.Sub, j, dst,
                Inst.And, j, (Immediate)0xF,
                Inst.Shr, j, (Immediate)2,
                Inst.Jz, smallEnd,

                Inst.Cmp, j, idx,
                Inst.Cmovg, j, idx,    // j = min(i, j).
                Inst.Sub, idx, j);     // i -= j.

            c.Bind(smallLoop).Emit( // Small loop.
                Inst.Movd, y0, Memory.Ptr(src),
                Inst.Movd, x0, Memory.Ptr(dst),

                Inst.Pcmpeqb, a0, a0,
                Inst.Pxor, a0, y0,
                Inst.Psrlw, a0, (Immediate)8,
                Inst.Punpcklbw, x0, cZero,

                Inst.Pshuflw, a0, a0, (Immediate)Utils.Shuffle(1, 1, 1, 1),
                Inst.Punpcklbw, y0, cZero,

                Inst.Pmullw, x0, a0,
                Inst.Paddsw, x0, cMul255A_0080,
                Inst.Pmulhuw, x0, cMul255M_0101,

                Inst.Paddw, x0, y0,
                Inst.Packuswb, x0, x0,

                Inst.Movd, Memory.Ptr(dst), x0,

                Inst.Add, dst, (Immediate)4,
                Inst.Add, src, (Immediate)4,

                Inst.Dec, j,
                Inst.Jnz, smallLoop).Bind(smallEnd);// Second section, prepare for an aligned loop.

            c.Emit(
                Inst.Test, idx, idx,
                Inst.Mov, j, idx,
                Inst.Jz, c.Exit,

                Inst.And, j, (Immediate)3,
                Inst.Shr, idx, (Immediate)2,
                Inst.Jz, largeEnd);

            c.Bind(largeLoop).Emit(// Aligned loop.
                Inst.Movups, y0, Memory.Ptr(src),
                Inst.Movaps, x0, Memory.Ptr(dst),

                Inst.Pcmpeqb, a0, a0,
                Inst.Xorps, a0, y0,
                Inst.Movaps, x1, x0,

                Inst.Psrlw, a0, (Immediate)8,
                Inst.Punpcklbw, x0, cZero,

                Inst.Movaps, a1, a0,
                Inst.Punpcklwd, a0, a0,

                Inst.Punpckhbw, x1, cZero,
                Inst.Punpckhwd, a1, a1,

                Inst.Pshufd, a0, a0, (Immediate)Utils.Shuffle(3, 3, 1, 1),
                Inst.Pshufd, a1, a1, (Immediate)Utils.Shuffle(3, 3, 1, 1),

                Inst.Pmullw, x0, a0,
                Inst.Pmullw, x1, a1,

                Inst.Paddsw, x0, cMul255A_0080,
                Inst.Paddsw, x1, cMul255A_0080,

                Inst.Pmulhuw, x0, cMul255M_0101,
                Inst.Pmulhuw, x1, cMul255M_0101,

                Inst.Add, src, (Immediate)16,
                Inst.Packuswb, x0, x1,

                Inst.Paddw, x0, y0,
                Inst.Movaps, Memory.Ptr(dst), x0,

                Inst.Add, dst, (Immediate)16,

                Inst.Dec, idx,
                Inst.Jnz, largeLoop).Bind(largeEnd);

            c.Emit(
                Inst.Test, j, j,
                Inst.Jnz, smallLoop);

            // Data
            c.Data(data, 16, 
                Data.Of(0x0080008000800080, 0x0080008000800080),
                Data.Of(0x0101010101010101, 0x0101010101010101));
        }

        protected override unsafe void Execute(Action<IntPtr, IntPtr, UIntPtr> fn, out string result, out string expected)
        {

            #region InitTestArguments
            var dst = new uint[] { 0x00000000, 0x10101010, 0x20100804, 0x30200003, 0x40204040, 0x5000004D, 0x60302E2C, 0x706F6E6D, 0x807F4F2F, 0x90349001, 0xA0010203, 0xB03204AB, 0xC023AFBD, 0xD0D0D0C0, 0xE0AABBCC, 0xFFFFFFFF, 0xF8F4F2F1 };
            var src = new uint[] { 0xE0E0E0E0, 0xA0008080, 0x341F1E1A, 0xFEFEFEFE, 0x80302010, 0x49490A0B, 0x998F7798, 0x00000000, 0x01010101, 0xA0264733, 0xBAB0B1B9, 0xFF000000, 0xDAB0A0C1, 0xE0BACFDA, 0x99887766, 0xFFFFFF80, 0xEE0A5FEC };

            int cnt = dst.Length;

            var rawptr = Marshal.AllocHGlobal((cnt + 3) * sizeof(uint) + 8);
            var pdst = new IntPtr(16 * (((long)rawptr + 15) / 16));

            rawptr = Marshal.AllocHGlobal((cnt + 3) * sizeof(uint) + 8);
            var psrc = new IntPtr(16 * (((long)rawptr + 15) / 16));

            UnsafeMemory.Copy(pdst, Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), cnt * sizeof(uint));
            UnsafeMemory.Copy(psrc, Marshal.UnsafeAddrOfPinnedArrayElement(src, 0), cnt * sizeof(uint));
            #endregion

            var expBuffer = new uint[cnt];
            for (var idx = 0; idx < cnt; idx++)
            {
                expBuffer[idx] = BlendSrcOver(dst[idx], src[idx]);				
            }

            fn(pdst, psrc, (UIntPtr)cnt);

            UnsafeMemory.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), pdst, cnt*sizeof(uint));

            result = string.Join(",", dst);
            expected = string.Join(",", expBuffer);
        }

        private static uint BlendSrcOver(uint dst, uint src)
        {
            var saInv = ~src >> 24;

            var d20 = (dst) & 0x00FF00FF;
            var d31 = (dst >> 8) & 0x00FF00FF;

            d20 *= saInv;
            d31 *= saInv;

            d20 = ((d20 + ((d20 >> 8) & 0x00FF00FFU) + 0x00800080U) & 0xFF00FF00U) >> 8;
            d31 = ((d31 + ((d31 >> 8) & 0x00FF00FFU) + 0x00800080U) & 0xFF00FF00U);

            return d20 + d31 + src;
        }
    }
}