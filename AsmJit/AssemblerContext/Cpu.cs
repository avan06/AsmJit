using System;
using AsmJit.Common;
using AsmJit.Common.Operands;

namespace AsmJit.AssemblerContext
{
    public static class Cpu
    {
        private static RipRegister _rip;
        private static SegRegister[] _seg;
        private static GpRegister[] _gpbLo;
        private static GpRegister[] _gpbHi;
        private static GpRegister[] _gpw;
        private static GpRegister[] _gpd;
        private static GpRegister[] _gpq;
        private static FpRegister[] _fp;
        private static MmRegister[] _mm;
        private static KRegister[] _k;
        private static XmmRegister[] _xmm;
        private static YmmRegister[] _ymm;
        private static ZmmRegister[] _zmm;

        static Cpu()
        {
            _rip = new RipRegister();
            _seg = new SegRegister[7].InitializeWith(i => new SegRegister(i));
            _gpbLo = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.GpbLo, i));
            _gpbHi = new GpRegister[4].InitializeWith(i => new GpRegister(GpRegisterType.GpbHi, i));
            _gpw = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpw, i));
            _gpd = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpd, i));
            _gpq = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpq, i));
            _fp = new FpRegister[8].InitializeWith(i => new FpRegister(i));
            _mm = new MmRegister[8].InitializeWith(i => new MmRegister(i));
            _k = new KRegister[8].InitializeWith(i => new KRegister(i));
            _xmm = new XmmRegister[32].InitializeWith(i => new XmmRegister(i));
            _ymm = new YmmRegister[32].InitializeWith(i => new YmmRegister(i));
            _zmm = new ZmmRegister[32].InitializeWith(i => new ZmmRegister(i));
        }

        internal static class Info
        {
            public static readonly int RegisterSize = Constants.X64 ? 8 : 4;
            public static readonly int StackAlignment = Constants.X64 ? 16 : IntPtr.Size;

            public static RegisterCount RegisterCount => RegisterCount.SystemDefault;
        }

        public static class Registers
        {
            public static RipRegister Rip => _rip;

            public static SegRegister Es => _seg[1];

            public static SegRegister Cs => _seg[2];

            public static SegRegister Ss => _seg[3];

            public static SegRegister Ds => _seg[4];

            public static SegRegister Fs => _seg[5];

            public static SegRegister Gs => _seg[6];

            public static GpRegister Al => _gpbLo[0];

            public static GpRegister Cl => _gpbLo[1];

            public static GpRegister Dl => _gpbLo[2];

            public static GpRegister Bl => _gpbLo[3];

            public static GpRegister Spl => _gpbLo[4];

            public static GpRegister Bpl => _gpbLo[5];

            public static GpRegister Sil => _gpbLo[6];

            public static GpRegister Dil => _gpbLo[7];

            public static GpRegister R8B => _gpbLo[8];

            public static GpRegister R9B => _gpbLo[9];

            public static GpRegister R10B => _gpbLo[10];

            public static GpRegister R11B => _gpbLo[11];

            public static GpRegister R12B => _gpbLo[12];

            public static GpRegister R13B => _gpbLo[13];

            public static GpRegister R14B => _gpbLo[14];

            public static GpRegister R15B => _gpbLo[15];

            public static GpRegister Ah => _gpbHi[0];

            public static GpRegister Ch => _gpbHi[1];

            public static GpRegister Dh => _gpbHi[2];

            public static GpRegister Bh => _gpbHi[3];

            public static GpRegister Ax => _gpw[0];

            public static GpRegister Cx => _gpw[1];

            public static GpRegister Dx => _gpw[2];

            public static GpRegister Bx => _gpw[3];

            public static GpRegister Sp => _gpw[4];

            public static GpRegister Bp => _gpw[5];

            public static GpRegister Si => _gpw[6];

            public static GpRegister Di => _gpw[7];

            public static GpRegister R8W => _gpw[8];

            public static GpRegister R9W => _gpw[9];

            public static GpRegister R10W => _gpw[10];

            public static GpRegister R11W => _gpw[11];

            public static GpRegister R12W => _gpw[12];

            public static GpRegister R13W => _gpw[13];

            public static GpRegister R14W => _gpw[14];

            public static GpRegister R15W => _gpw[15];

            public static GpRegister Eax => _gpd[0];

            public static GpRegister Ecx => _gpd[1];

            public static GpRegister Edx => _gpd[2];

            public static GpRegister Ebx => _gpd[3];

            public static GpRegister Esp => _gpd[4];

            public static GpRegister Ebp => _gpd[5];

            public static GpRegister Esi => _gpd[6];

            public static GpRegister Edi => _gpd[7];

            public static GpRegister R8D => _gpd[8];

            public static GpRegister R9D => _gpd[9];

            public static GpRegister R10D => _gpd[10];

            public static GpRegister R11D => _gpd[11];

            public static GpRegister R12D => _gpd[12];

            public static GpRegister R13D => _gpd[13];

            public static GpRegister R14D => _gpd[14];

            public static GpRegister R15D => _gpd[15];

            public static GpRegister Rax => _gpq[0];

            public static GpRegister Rcx => _gpq[1];

            public static GpRegister Rdx => _gpq[2];

            public static GpRegister Rbx => _gpq[3];

            public static GpRegister Rsp => _gpq[4];

            public static GpRegister Rbp => _gpq[5];

            public static GpRegister Rsi => _gpq[6];

            public static GpRegister Rdi => _gpq[7];

            public static GpRegister R8 => _gpq[8];

            public static GpRegister R9 => _gpq[9];

            public static GpRegister R10 => _gpq[10];

            public static GpRegister R11 => _gpq[11];

            public static GpRegister R12 => _gpq[12];

            public static GpRegister R13 => _gpq[13];

            public static GpRegister R14 => _gpq[14];

            public static GpRegister R15 => _gpq[15];

            public static FpRegister Fp0 => _fp[0];

            public static FpRegister Fp1 => _fp[1];

            public static FpRegister Fp2 => _fp[2];

            public static FpRegister Fp3 => _fp[3];

            public static FpRegister Fp4 => _fp[4];

            public static FpRegister Fp5 => _fp[5];

            public static FpRegister Fp6 => _fp[6];

            public static FpRegister Fp7 => _fp[7];

            public static MmRegister Mm0 => _mm[0];

            public static MmRegister Mm1 => _mm[1];

            public static MmRegister Mm2 => _mm[2];

            public static MmRegister Mm3 => _mm[3];

            public static MmRegister Mm4 => _mm[4];

            public static MmRegister Mm5 => _mm[5];

            public static MmRegister Mm6 => _mm[6];

            public static MmRegister Mm7 => _mm[7];

            public static KRegister K0 => _k[0];

            public static KRegister K1 => _k[1];

            public static KRegister K2 => _k[2];

            public static KRegister K3 => _k[3];

            public static KRegister K4 => _k[4];

            public static KRegister K5 => _k[5];

            public static KRegister K6 => _k[6];

            public static KRegister K7 => _k[7];

            public static XmmRegister Xmm0 => _xmm[0];

            public static XmmRegister Xmm1 => _xmm[1];

            public static XmmRegister Xmm2 => _xmm[2];

            public static XmmRegister Xmm3 => _xmm[3];

            public static XmmRegister Xmm4 => _xmm[4];

            public static XmmRegister Xmm5 => _xmm[5];

            public static XmmRegister Xmm6 => _xmm[6];

            public static XmmRegister Xmm7 => _xmm[7];

            public static XmmRegister Xmm8 => _xmm[8];

            public static XmmRegister Xmm9 => _xmm[9];

            public static XmmRegister Xmm10 => _xmm[10];

            public static XmmRegister Xmm11 => _xmm[11];

            public static XmmRegister Xmm12 => _xmm[12];

            public static XmmRegister Xmm13 => _xmm[13];

            public static XmmRegister Xmm14 => _xmm[14];

            public static XmmRegister Xmm15 => _xmm[15];

            public static XmmRegister Xmm16 => _xmm[16];

            public static XmmRegister Xmm17 => _xmm[17];

            public static XmmRegister Xmm18 => _xmm[18];

            public static XmmRegister Xmm19 => _xmm[19];

            public static XmmRegister Xmm20 => _xmm[20];

            public static XmmRegister Xmm21 => _xmm[21];

            public static XmmRegister Xmm22 => _xmm[22];

            public static XmmRegister Xmm23 => _xmm[23];

            public static XmmRegister Xmm24 => _xmm[24];

            public static XmmRegister Xmm25 => _xmm[25];

            public static XmmRegister Xmm26 => _xmm[26];

            public static XmmRegister Xmm27 => _xmm[27];

            public static XmmRegister Xmm28 => _xmm[28];

            public static XmmRegister Xmm29 => _xmm[29];

            public static XmmRegister Xmm30 => _xmm[30];

            public static XmmRegister Xmm31 => _xmm[31];

            public static YmmRegister Ymm0 => _ymm[0];

            public static YmmRegister Ymm1 => _ymm[1];

            public static YmmRegister Ymm2 => _ymm[2];

            public static YmmRegister Ymm3 => _ymm[3];

            public static YmmRegister Ymm4 => _ymm[4];

            public static YmmRegister Ymm5 => _ymm[5];

            public static YmmRegister Ymm6 => _ymm[6];

            public static YmmRegister Ymm7 => _ymm[7];

            public static YmmRegister Ymm8 => _ymm[8];

            public static YmmRegister Ymm9 => _ymm[9];

            public static YmmRegister Ymm10 => _ymm[10];

            public static YmmRegister Ymm11 => _ymm[11];

            public static YmmRegister Ymm12 => _ymm[12];

            public static YmmRegister Ymm13 => _ymm[13];

            public static YmmRegister Ymm14 => _ymm[14];

            public static YmmRegister Ymm15 => _ymm[15];

            public static YmmRegister Ymm16 => _ymm[16];

            public static YmmRegister Ymm17 => _ymm[17];

            public static YmmRegister Ymm18 => _ymm[18];

            public static YmmRegister Ymm19 => _ymm[19];

            public static YmmRegister Ymm20 => _ymm[20];

            public static YmmRegister Ymm21 => _ymm[21];

            public static YmmRegister Ymm22 => _ymm[22];

            public static YmmRegister Ymm23 => _ymm[23];

            public static YmmRegister Ymm24 => _ymm[24];

            public static YmmRegister Ymm25 => _ymm[25];

            public static YmmRegister Ymm26 => _ymm[26];

            public static YmmRegister Ymm27 => _ymm[27];

            public static YmmRegister Ymm28 => _ymm[28];

            public static YmmRegister Ymm29 => _ymm[29];

            public static YmmRegister Ymm30 => _ymm[30];

            public static YmmRegister Ymm31 => _ymm[31];

            public static ZmmRegister Zmm0 => _zmm[0];

            public static ZmmRegister Zmm1 => _zmm[1];

            public static ZmmRegister Zmm2 => _zmm[2];

            public static ZmmRegister Zmm3 => _zmm[3];

            public static ZmmRegister Zmm4 => _zmm[4];

            public static ZmmRegister Zmm5 => _zmm[5];

            public static ZmmRegister Zmm6 => _zmm[6];

            public static ZmmRegister Zmm7 => _zmm[7];

            public static ZmmRegister Zmm8 => _zmm[8];

            public static ZmmRegister Zmm9 => _zmm[9];

            public static ZmmRegister Zmm10 => _zmm[10];

            public static ZmmRegister Zmm11 => _zmm[11];

            public static ZmmRegister Zmm12 => _zmm[12];

            public static ZmmRegister Zmm13 => _zmm[13];

            public static ZmmRegister Zmm14 => _zmm[14];

            public static ZmmRegister Zmm15 => _zmm[15];

            public static ZmmRegister Zmm16 => _zmm[16];

            public static ZmmRegister Zmm17 => _zmm[17];

            public static ZmmRegister Zmm18 => _zmm[18];

            public static ZmmRegister Zmm19 => _zmm[19];

            public static ZmmRegister Zmm20 => _zmm[20];

            public static ZmmRegister Zmm21 => _zmm[21];

            public static ZmmRegister Zmm22 => _zmm[22];

            public static ZmmRegister Zmm23 => _zmm[23];

            public static ZmmRegister Zmm24 => _zmm[24];

            public static ZmmRegister Zmm25 => _zmm[25];

            public static ZmmRegister Zmm26 => _zmm[26];

            public static ZmmRegister Zmm27 => _zmm[27];

            public static ZmmRegister Zmm28 => _zmm[28];

            public static ZmmRegister Zmm29 => _zmm[29];

            public static ZmmRegister Zmm30 => _zmm[30];

            public static ZmmRegister Zmm31 => _zmm[31];
        }
    }
}
