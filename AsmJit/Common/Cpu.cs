using System;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;

namespace AsmJit.Common
{
    public static class Cpu
    {
        private static readonly RipRegister _rip;
        private static readonly SegRegister[] _seg;
        private static readonly GpRegister[] _gpbLo;
        private static readonly GpRegister[] _gpbHi;
        private static readonly GpRegister[] _gpw;
        private static readonly GpRegister[] _gpd;
        private static readonly GpRegister[] _gpq;
        private static readonly FpRegister[] _fp;
        private static readonly MmRegister[] _mm;
        private static readonly KRegister[] _k;
        private static readonly XmmRegister[] _xmm;
        private static readonly YmmRegister[] _ymm;
        private static readonly ZmmRegister[] _zmm;

        internal static readonly GpRegister Zax;
        internal static readonly GpRegister Zcx;
        internal static readonly GpRegister Zdx;
        internal static readonly GpRegister Zbx;
        internal static readonly GpRegister Zsp;
        internal static readonly GpRegister Zbp;
        internal static readonly GpRegister Zsi;
        internal static readonly GpRegister Zdi;

        public static readonly RegistersClass Registers;

        static Cpu()
        {
            _rip   = new RipRegister();
            _seg   = new SegRegister[7].InitializeWith(i => new SegRegister(i));
            _gpbLo = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.GpbLo, i));
            _gpbHi = new GpRegister[4].InitializeWith(i => new GpRegister(GpRegisterType.GpbHi, i));
            _gpw   = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpw, i));
            _gpd   = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpd, i));
            _gpq   = new GpRegister[16].InitializeWith(i => new GpRegister(GpRegisterType.Gpq, i));
            _fp    = new FpRegister[8].InitializeWith(i => new FpRegister(i));
            _mm    = new MmRegister[8].InitializeWith(i => new MmRegister(i));
            _k     = new KRegister[8].InitializeWith(i => new KRegister(i));
            _xmm   = new XmmRegister[32].InitializeWith(i => new XmmRegister(i));
            _ymm   = new YmmRegister[32].InitializeWith(i => new YmmRegister(i));
            _zmm   = new ZmmRegister[32].InitializeWith(i => new ZmmRegister(i));

            Registers = new RegistersClass();

            Zdi = new GpRegister(Constants.X64 ? Registers.Rdi : Registers.Edi);
            Zsi = new GpRegister(Constants.X64 ? Registers.Rsi : Registers.Esi);
            Zbp = new GpRegister(Constants.X64 ? Registers.Rbp : Registers.Ebp);
            Zsp = new GpRegister(Constants.X64 ? Registers.Rsp : Registers.Esp);
            Zbx = new GpRegister(Constants.X64 ? Registers.Rbx : Registers.Ebx);
            Zdx = new GpRegister(Constants.X64 ? Registers.Rdx : Registers.Edx);
            Zcx = new GpRegister(Constants.X64 ? Registers.Rcx : Registers.Ecx);
            Zax = new GpRegister(Constants.X64 ? Registers.Rax : Registers.Eax);
        }

        internal static GpRegister GpbLo(int index) => new GpRegister(GpRegisterType.GpbLo, index);

        internal static GpRegister GpbHi(int index) => new GpRegister(GpRegisterType.GpbHi, index);

        internal static GpRegister Gpw(int index) => new GpRegister(GpRegisterType.Gpw, index);

        internal static GpRegister Gpd(int index) => new GpRegister(GpRegisterType.Gpd, index);

        internal static GpRegister Gpq(int index) => new GpRegister(GpRegisterType.Gpq, index);

        internal static FpRegister Fp(int index) => new FpRegister(index);

        internal static MmRegister Mm(int index) => new MmRegister(index);

        internal static KRegister K(int index) => new KRegister(index);

        internal static XmmRegister Xmm(int index) => new XmmRegister(index);

        internal static YmmRegister Ymm(int index) => new YmmRegister(index);

        internal static ZmmRegister Zmm(int index) => new ZmmRegister(index);

        internal static class Info
        {
            public static readonly int RegisterSize = Constants.X64 ? 8 : 4;
            public static readonly int StackAlignment = Constants.X64 ? 16 : IntPtr.Size;

            public static RegisterCount RegisterCount => RegisterCount.SystemDefault;
        }

        public class RegistersClass
        {
            public readonly RipRegister Rip = _rip;

            public readonly SegRegister Es = _seg[1];

            public readonly SegRegister Cs = _seg[2];

            public readonly SegRegister Ss = _seg[3];

            public readonly SegRegister Ds = _seg[4];

            public readonly SegRegister Fs = _seg[5];

            public readonly SegRegister Gs = _seg[6];

            public readonly GpRegister Al = _gpbLo[0];

            public readonly GpRegister Cl = _gpbLo[1];

            public readonly GpRegister Dl = _gpbLo[2];

            public readonly GpRegister Bl = _gpbLo[3];

            public readonly GpRegister Spl = _gpbLo[4];

            public readonly GpRegister Bpl = _gpbLo[5];

            public readonly GpRegister Sil = _gpbLo[6];

            public readonly GpRegister Dil = _gpbLo[7];

            public readonly GpRegister R8B = _gpbLo[8];

            public readonly GpRegister R9B = _gpbLo[9];

            public readonly GpRegister R10B = _gpbLo[10];

            public readonly GpRegister R11B = _gpbLo[11];

            public readonly GpRegister R12B = _gpbLo[12];

            public readonly GpRegister R13B = _gpbLo[13];

            public readonly GpRegister R14B = _gpbLo[14];

            public readonly GpRegister R15B = _gpbLo[15];

            public readonly GpRegister Ah = _gpbHi[0];

            public readonly GpRegister Ch = _gpbHi[1];

            public readonly GpRegister Dh = _gpbHi[2];

            public readonly GpRegister Bh = _gpbHi[3];

            public readonly GpRegister Ax = _gpw[0];

            public readonly GpRegister Cx = _gpw[1];

            public readonly GpRegister Dx = _gpw[2];

            public readonly GpRegister Bx = _gpw[3];

            public readonly GpRegister Sp = _gpw[4];

            public readonly GpRegister Bp = _gpw[5];

            public readonly GpRegister Si = _gpw[6];

            public readonly GpRegister Di = _gpw[7];

            public readonly GpRegister R8W = _gpw[8];

            public readonly GpRegister R9W = _gpw[9];

            public readonly GpRegister R10W = _gpw[10];

            public readonly GpRegister R11W = _gpw[11];

            public readonly GpRegister R12W = _gpw[12];

            public readonly GpRegister R13W = _gpw[13];

            public readonly GpRegister R14W = _gpw[14];

            public readonly GpRegister R15W = _gpw[15];

            public readonly GpRegister Eax = _gpd[0];

            public readonly GpRegister Ecx = _gpd[1];

            public readonly GpRegister Edx = _gpd[2];

            public readonly GpRegister Ebx = _gpd[3];

            public readonly GpRegister Esp = _gpd[4];

            public readonly GpRegister Ebp = _gpd[5];

            public readonly GpRegister Esi = _gpd[6];

            public readonly GpRegister Edi = _gpd[7];

            public readonly GpRegister R8D = _gpd[8];

            public readonly GpRegister R9D = _gpd[9];

            public readonly GpRegister R10D = _gpd[10];

            public readonly GpRegister R11D = _gpd[11];

            public readonly GpRegister R12D = _gpd[12];

            public readonly GpRegister R13D = _gpd[13];

            public readonly GpRegister R14D = _gpd[14];

            public readonly GpRegister R15D = _gpd[15];

            public readonly GpRegister Rax = _gpq[0];

            public readonly GpRegister Rcx = _gpq[1];

            public readonly GpRegister Rdx = _gpq[2];

            public readonly GpRegister Rbx = _gpq[3];

            public readonly GpRegister Rsp = _gpq[4];

            public readonly GpRegister Rbp = _gpq[5];

            public readonly GpRegister Rsi = _gpq[6];

            public readonly GpRegister Rdi = _gpq[7];

            public readonly GpRegister R8 = _gpq[8];

            public readonly GpRegister R9 = _gpq[9];

            public readonly GpRegister R10 = _gpq[10];

            public readonly GpRegister R11 = _gpq[11];

            public readonly GpRegister R12 = _gpq[12];

            public readonly GpRegister R13 = _gpq[13];

            public readonly GpRegister R14 = _gpq[14];

            public readonly GpRegister R15 = _gpq[15];

            public readonly FpRegister Fp0 = _fp[0];

            public readonly FpRegister Fp1 = _fp[1];

            public readonly FpRegister Fp2 = _fp[2];

            public readonly FpRegister Fp3 = _fp[3];

            public readonly FpRegister Fp4 = _fp[4];

            public readonly FpRegister Fp5 = _fp[5];

            public readonly FpRegister Fp6 = _fp[6];

            public readonly FpRegister Fp7 = _fp[7];

            public readonly MmRegister Mm0 = _mm[0];

            public readonly MmRegister Mm1 = _mm[1];

            public readonly MmRegister Mm2 = _mm[2];

            public readonly MmRegister Mm3 = _mm[3];

            public readonly MmRegister Mm4 = _mm[4];

            public readonly MmRegister Mm5 = _mm[5];

            public readonly MmRegister Mm6 = _mm[6];

            public readonly MmRegister Mm7 = _mm[7];

            public readonly KRegister K0 = _k[0];

            public readonly KRegister K1 = _k[1];

            public readonly KRegister K2 = _k[2];

            public readonly KRegister K3 = _k[3];

            public readonly KRegister K4 = _k[4];

            public readonly KRegister K5 = _k[5];

            public readonly KRegister K6 = _k[6];

            public readonly KRegister K7 = _k[7];

            public readonly XmmRegister Xmm0 = _xmm[0];

            public readonly XmmRegister Xmm1 = _xmm[1];

            public readonly XmmRegister Xmm2 = _xmm[2];

            public readonly XmmRegister Xmm3 = _xmm[3];

            public readonly XmmRegister Xmm4 = _xmm[4];

            public readonly XmmRegister Xmm5 = _xmm[5];

            public readonly XmmRegister Xmm6 = _xmm[6];

            public readonly XmmRegister Xmm7 = _xmm[7];

            public readonly XmmRegister Xmm8 = _xmm[8];

            public readonly XmmRegister Xmm9 = _xmm[9];

            public readonly XmmRegister Xmm10 = _xmm[10];

            public readonly XmmRegister Xmm11 = _xmm[11];

            public readonly XmmRegister Xmm12 = _xmm[12];

            public readonly XmmRegister Xmm13 = _xmm[13];

            public readonly XmmRegister Xmm14 = _xmm[14];

            public readonly XmmRegister Xmm15 = _xmm[15];

            public readonly XmmRegister Xmm16 = _xmm[16];

            public readonly XmmRegister Xmm17 = _xmm[17];

            public readonly XmmRegister Xmm18 = _xmm[18];

            public readonly XmmRegister Xmm19 = _xmm[19];

            public readonly XmmRegister Xmm20 = _xmm[20];

            public readonly XmmRegister Xmm21 = _xmm[21];

            public readonly XmmRegister Xmm22 = _xmm[22];

            public readonly XmmRegister Xmm23 = _xmm[23];

            public readonly XmmRegister Xmm24 = _xmm[24];

            public readonly XmmRegister Xmm25 = _xmm[25];

            public readonly XmmRegister Xmm26 = _xmm[26];

            public readonly XmmRegister Xmm27 = _xmm[27];

            public readonly XmmRegister Xmm28 = _xmm[28];

            public readonly XmmRegister Xmm29 = _xmm[29];

            public readonly XmmRegister Xmm30 = _xmm[30];

            public readonly XmmRegister Xmm31 = _xmm[31];

            public readonly YmmRegister Ymm0 = _ymm[0];

            public readonly YmmRegister Ymm1 = _ymm[1];

            public readonly YmmRegister Ymm2 = _ymm[2];

            public readonly YmmRegister Ymm3 = _ymm[3];

            public readonly YmmRegister Ymm4 = _ymm[4];

            public readonly YmmRegister Ymm5 = _ymm[5];

            public readonly YmmRegister Ymm6 = _ymm[6];

            public readonly YmmRegister Ymm7 = _ymm[7];

            public readonly YmmRegister Ymm8 = _ymm[8];

            public readonly YmmRegister Ymm9 = _ymm[9];

            public readonly YmmRegister Ymm10 = _ymm[10];

            public readonly YmmRegister Ymm11 = _ymm[11];

            public readonly YmmRegister Ymm12 = _ymm[12];

            public readonly YmmRegister Ymm13 = _ymm[13];

            public readonly YmmRegister Ymm14 = _ymm[14];

            public readonly YmmRegister Ymm15 = _ymm[15];

            public readonly YmmRegister Ymm16 = _ymm[16];

            public readonly YmmRegister Ymm17 = _ymm[17];

            public readonly YmmRegister Ymm18 = _ymm[18];

            public readonly YmmRegister Ymm19 = _ymm[19];

            public readonly YmmRegister Ymm20 = _ymm[20];

            public readonly YmmRegister Ymm21 = _ymm[21];

            public readonly YmmRegister Ymm22 = _ymm[22];

            public readonly YmmRegister Ymm23 = _ymm[23];

            public readonly YmmRegister Ymm24 = _ymm[24];

            public readonly YmmRegister Ymm25 = _ymm[25];

            public readonly YmmRegister Ymm26 = _ymm[26];

            public readonly YmmRegister Ymm27 = _ymm[27];

            public readonly YmmRegister Ymm28 = _ymm[28];

            public readonly YmmRegister Ymm29 = _ymm[29];

            public readonly YmmRegister Ymm30 = _ymm[30];

            public readonly YmmRegister Ymm31 = _ymm[31];

            public readonly ZmmRegister Zmm0 = _zmm[0];

            public readonly ZmmRegister Zmm1 = _zmm[1];

            public readonly ZmmRegister Zmm2 = _zmm[2];

            public readonly ZmmRegister Zmm3 = _zmm[3];

            public readonly ZmmRegister Zmm4 = _zmm[4];

            public readonly ZmmRegister Zmm5 = _zmm[5];

            public readonly ZmmRegister Zmm6 = _zmm[6];

            public readonly ZmmRegister Zmm7 = _zmm[7];

            public readonly ZmmRegister Zmm8 = _zmm[8];

            public readonly ZmmRegister Zmm9 = _zmm[9];

            public readonly ZmmRegister Zmm10 = _zmm[10];

            public readonly ZmmRegister Zmm11 = _zmm[11];

            public readonly ZmmRegister Zmm12 = _zmm[12];

            public readonly ZmmRegister Zmm13 = _zmm[13];

            public readonly ZmmRegister Zmm14 = _zmm[14];

            public readonly ZmmRegister Zmm15 = _zmm[15];

            public readonly ZmmRegister Zmm16 = _zmm[16];

            public readonly ZmmRegister Zmm17 = _zmm[17];

            public readonly ZmmRegister Zmm18 = _zmm[18];

            public readonly ZmmRegister Zmm19 = _zmm[19];

            public readonly ZmmRegister Zmm20 = _zmm[20];

            public readonly ZmmRegister Zmm21 = _zmm[21];

            public readonly ZmmRegister Zmm22 = _zmm[22];

            public readonly ZmmRegister Zmm23 = _zmm[23];

            public readonly ZmmRegister Zmm24 = _zmm[24];

            public readonly ZmmRegister Zmm25 = _zmm[25];

            public readonly ZmmRegister Zmm26 = _zmm[26];

            public readonly ZmmRegister Zmm27 = _zmm[27];

            public readonly ZmmRegister Zmm28 = _zmm[28];

            public readonly ZmmRegister Zmm29 = _zmm[29];

            public readonly ZmmRegister Zmm30 = _zmm[30];

            public readonly ZmmRegister Zmm31 = _zmm[31];
        }
    }
}
