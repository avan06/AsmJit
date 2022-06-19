using System;
using AsmJit.Common.Enums;

namespace AsmJit.Common
{
    internal static class Constants
    {
        public static readonly bool X64 = IntPtr.Size > 4;
        public static readonly CallingConvention DefaultCallingConvention = X64 ? CallingConvention.X64Win : CallingConvention.X86StdCall;

        public const int InvalidValue = -1;
        public const int InvalidId    = InvalidValue;

        public static class X86
        {
            /// <summary>
            /// X86/X64 index register legacy and AVX2 (VSIB) support.
            /// </summary>
            public const int MemVSibGpz = 0;
            public const int MemVSibXmm = 1;
            public const int MemVSibYmm = 2;
            public const int MemVSibZmm = 3;

            /// <summary>
            /// X86/X64 specific memory flags.
            /// </summary>
            public const int MemSegBits    = 0x7;
            public const int MemSegIndex   = 0;
            public const int MemSegMask    = MemSegBits << MemSegIndex;
            public const int MemGpdBits    = 0x1;
            public const int MemGpdIndex   = 3;
            public const int MemGpdMask    = MemGpdBits << MemGpdIndex;
            public const int MemVSibBits   = 0x3;
            public const int MemVSibIndex  = 4;
            public const int MemVSibMask   = MemVSibBits << MemVSibIndex;
            public const int MemShiftBits  = 0x3;
            public const int MemShiftIndex = 6;
            public const int MemShiftMask  = MemShiftBits << MemShiftIndex;

            /// <summary>
            /// MM & VEX & EVEX & XOP
            /// 
            /// `MM` field is used to encode prefixes needed by the instruction or as a part of VEX/EVEX prefix.
            /// Described as `mm` and `mmmmm` in instruction manuals.
            /// ---------------------
            ///
            /// Two meanings:
            ///  * Part of a legacy opcode (prefixes emitted before the main opcode byte).
            ///  * `MMMMM` field in VEX|EVEX|XOP instruction.
            ///
            /// AVX reserves 5 bits for `MMMMM` field, however AVX instructions only use 2 bits and XOP 3 bits.
            /// AVX-512 shrinks `MMMMM` field into `MMM` so it's safe to use [4:3] bits of `MMMMM` field for internal payload.
            ///
            /// AsmJit divides MMMMM field into this layout:
            ///
            /// [2:0] - Used to describe 0F, 0F38 and 0F3A legacy prefix bytes and 3 bits of MMMMM field for XOP/AVX/AVX512.
            /// [3]   - Required by XOP instructions, so we use this bit also to indicate that this is a XOP opcode.
            /// [4]   - Used to force EVEX prefix - this bit is not used by any X86 instruction yet, so AsmJit uses it to
            ///         describe EVEX only instructions or sets its bit when user uses InstOptions::kX86_Evex to force EVEX.
            /// </summary>
            public const long InstOpCode_MM_Shift = 16;
            /// <summary>
            /// F0000
            /// </summary>
            public const long InstOpCode_MM_Mask = 0x0FU << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 00000
            /// </summary>
            public const long InstOpCode_MM_0000 = 0x00U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 10000
            /// </summary>
            public const long InstOpCode_MM_0F00 = 0x01U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 20000
            /// </summary>
            public const long InstOpCode_MM_0F38 = 0x02U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 30000
            /// </summary>
            public const long InstOpCode_MM_0F3A = 0x03U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 30000
            /// </summary>
            public const long InstOpCode_MM_00011 = 0x03U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 80000
            /// </summary>
            public const long InstOpCode_MM_01000 = 0x08U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// 90000
            /// </summary>
            public const long InstOpCode_MM_01001 = 0x09U << (int)InstOpCode_MM_Shift;
            /// <summary>
            /// F0000
            /// </summary>
            public const long InstOpCode_MM_0F01 = 0x0FU << (int)InstOpCode_MM_Shift;

            /// <summary>
            /// `PP` Field
            /// 
            /// `PP` field is used to encode prefixes needed by the instruction or as a part of VEX/EVEX prefix.
            ///  Described as `pp` in instruction manuals.
            /// ----------
            /// These fields are stored deliberately right after each other as it makes it easier to
            /// construct VEX prefix from the opcode value stored in the instruction database.
            /// 
            /// Two meanings:
            ///   * "PP" field in AVX/XOP/AVX-512 instruction.
            ///   * Mandatory Prefix in legacy encoding.
            /// 
            /// AVX reserves 2 bits for `PP` field, but AsmJit extends the storage by 1 more bit that is used to emit 9B prefix for some X87-FPU instructions.
            /// </summary>
            public const long InstOpCode_PP_Shift = 20;
            /// <summary>
            /// 700000
            /// </summary>
            public const long InstOpCode_PP_Mask = 0x07U << (int)InstOpCode_PP_Shift;
            /// <summary>
            /// 000000
            /// </summary>
            public const long InstOpCode_PP_00 = 0x00U << (int)InstOpCode_PP_Shift;
            /// <summary>
            /// 100000
            /// </summary>
            public const long InstOpCode_PP_66 = 0x01U << (int)InstOpCode_PP_Shift;
            /// <summary>
            /// 200000
            /// </summary>
            public const long InstOpCode_PP_F3 = 0x02U << (int)InstOpCode_PP_Shift;
            /// <summary>
            /// 300000
            /// </summary>
            public const long InstOpCode_PP_F2 = 0x03U << (int)InstOpCode_PP_Shift;
            /// <summary>
            /// 700000
            /// </summary>
            public const long InstOpCode_PP_9B = 0x07U << (int)InstOpCode_PP_Shift;

            /// <summary>
            /// `L` or `LL` field in AVX/XOP/AVX-512
            /// 
            /// `LL` field is used exclusively by AVX+ and AVX512+ instruction sets. It describes vector size,
            /// which is `L.128` for XMM register,`L.256` for for YMM register, and `L.512` for ZMM register.
            /// The `LL` field is omitted in case that instruction supports multiple vector lengths,
            /// however, if the instruction requires specific `L` value it must be specified as a part of the opcode.
            /// ----------
            /// VEX/XOP prefix can only use the first bit `L.128` or `L.256`.
            /// EVEX prefix prefix makes it possible to use also `L.512`.
            /// If the instruction set manual describes an instruction by `LIG`
            /// it means that the `L` field is ignored and AsmJit defaults to `0` in such case.
            /// </summary>
            public const long InstOpCode_L_Shift  = 23;
            /// <summary>
            /// 1800000
            /// </summary>
            public const long InstOpCode_L_Mask = 0x03U << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 0000000
            /// </summary>
            public const long InstOpCode_L_128 = 0x00U << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 0800000
            /// </summary>
            public const long InstOpCode_L_256 = 0x01U << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 1000000
            /// </summary>
            public const long InstOpCode_L_512 = 0x02U << (int)InstOpCode_L_Shift;

            /// <summary>
            /// `O` Field in ModR/M
            /// 
            /// `O` field is an extended opcode field (3 bits) embedded in ModR/M BYTE.
            /// </summary>
            public const long InstOpCode_O_Shift  = 25;
            /// <summary>
            /// E000000
            /// </summary>
            public const long InstOpCode_O_Mask = 0x07U << (int)InstOpCode_O_Shift;

            /// <summary>
            /// EVEX.W Field
            /// 
            /// `W` field used by EVEX instruction encoding.
            /// </summary>
            public const long InstOpCode_EW_Shift = 30;
            /// <summary>
            /// 40000000
            /// </summary>
            public const long InstOpCode_EW_Mask = 0x01U << (int)InstOpCode_EW_Shift;
            /// <summary>
            /// 40000000
            /// </summary>
            public const long InstOpCode_EW = 0x01U << (int)InstOpCode_EW_Shift;

            /// <summary>
            /// `W` field is the most complicated. It was added by 64-bit architecture to promote default operation width
            /// (instructions that perform 32-bit operation by default require to override the width to 64-bit explicitly).
            /// There is nothing wrong on this, however, some instructions introduced implicit `W` override,
            /// for example a `cdqe` instruction is basically a `cwde` instruction with overridden `W` (set to 1).
            /// There are some others in the base X86 instruction set.
            /// More recent instruction sets started using `W` field more often:
            /// AVX instructions started using `W` field as an extended opcode for FMA, GATHER, PERM, and other instructions.
            /// It also uses `W` field to override the default operation width in instructions like `vmovq`.
            /// 
            /// AVX-512 instructions started using `W` field as an extended opcode for all new instructions.
            /// This wouldn't have been an issue if the `W` field of AVX-512 have matched AVX, but this is not always the case.
            /// </summary>
            public const long InstOpCode_W_Shift  = 31;
            /// <summary>
            /// 80000000
            /// </summary>
            public const long InstOpCode_W_Mask = 0x01U << (int)InstOpCode_W_Shift;
            /// <summary>
            /// 80000000
            /// </summary>
            public const long InstOpCode_W = 0x01U << (int)InstOpCode_W_Shift;

            /// <summary>
            /// 000000
            /// </summary>
            public const long InstTable_L__ = 0L << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 000000
            /// </summary>
            public const long InstTable_L_I = 0L << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 000000
            /// </summary>
            public const long InstTable_L_0 = 0L << (int)InstOpCode_L_Shift;
            /// <summary>
            /// 800000
            /// </summary>
            public const long InstTable_L_L = 1L << (int)InstOpCode_L_Shift;

            /// <summary>
            /// 00000000 REX.W/VEX.W is unspecified.
            /// </summary>
            public const long InstTable_W__ = 0L << (int)InstOpCode_W_Shift;
            /// <summary>
            /// 00000000 REX.W/VEX.W is ignored (WIG).
            /// </summary>
            public const long InstTable_W_I = 0L << (int)InstOpCode_W_Shift;
            /// <summary>
            /// 00000000 REX.W/VEX.W is 0 (W0).
            /// </summary>
            public const long InstTable_W_0 = 0L << (int)InstOpCode_W_Shift;
            /// <summary>
            /// 80000000 REX.W/VEX.W is 1 (W1).
            /// </summary>
            public const long InstTable_W_W = 1L << (int)InstOpCode_W_Shift;

            /// <summary>
            /// 00000000 EVEX.W is unspecified (not EVEX instruction).
            /// </summary>
            public const long InstTable_E__ = 0L << (int)InstOpCode_EW_Shift;
            /// <summary>
            /// 00000000 EVEX.W is ignored (WIG).
            /// </summary>
            public const long InstTable_E_I = 0L << (int)InstOpCode_EW_Shift;
            /// <summary>
            /// 00000000 EVEX.W is 0 (W0).
            /// </summary>
            public const long InstTable_E_0 = 0L << (int)InstOpCode_EW_Shift;
            /// <summary>
            /// 40000000 EVEX.W is 1 (W1).
            /// </summary>
            public const long InstTable_E_1 = 1L << (int)InstOpCode_EW_Shift;

            /// <summary>
            /// X86InstOp
            /// </summary>
            public const long InstOpGb    = 0x0001;
            public const long InstOpGw    = 0x0002;
            public const long InstOpGd    = 0x0004;
            public const long InstOpGq    = 0x0008;
            public const long InstOpFp    = 0x0010;
            public const long InstOpMm    = 0x0020;
            public const long InstOpK     = 0x0040;
            public const long InstOpXmm   = 0x0100;
            public const long InstOpYmm   = 0x0200;
            public const long InstOpZmm   = 0x0400;
            public const long InstOpMem   = 0x1000;
            public const long InstOpImm   = 0x2000;
            public const long InstOpLabel = 0x4000;
            public const long InstOpNone  = 0x8000;

            /// <summary>
            /// X86InstOpInternal
            /// </summary>
            public const long InstOpI        = InstOpImm;
            public const long InstOpL        = InstOpLabel;
            public const long InstOpLImm     = InstOpLabel | InstOpImm;
            public const long InstOpGwb      = InstOpGw    | InstOpGb;
            public const long InstOpGqd      = InstOpGq    | InstOpGd;
            public const long InstOpGqdw     = InstOpGq    | InstOpGd | InstOpGw;
            public const long InstOpGqdwb    = InstOpGq    | InstOpGd | InstOpGw | InstOpGb;
            public const long InstOpGbMem    = InstOpGb    | InstOpMem;
            public const long InstOpGwMem    = InstOpGw    | InstOpMem;
            public const long InstOpGdMem    = InstOpGd    | InstOpMem;
            public const long InstOpGqMem    = InstOpGq    | InstOpMem;
            public const long InstOpGwbMem   = InstOpGwb   | InstOpMem;
            public const long InstOpGqdMem   = InstOpGqd   | InstOpMem;
            public const long InstOpGqdwMem  = InstOpGqdw  | InstOpMem;
            public const long InstOpGqdwbMem = InstOpGqdwb | InstOpMem;
            public const long InstOpFpMem    = InstOpFp    | InstOpMem;
            public const long InstOpMmMem    = InstOpMm    | InstOpMem;
            public const long InstOpKMem     = InstOpK     | InstOpMem;
            public const long InstOpXmmMem   = InstOpXmm   | InstOpMem;
            public const long InstOpYmmMem   = InstOpYmm   | InstOpMem;
            public const long InstOpZmmMem   = InstOpZmm   | InstOpMem;
            public const long InstOpMmXmm    = InstOpMm    | InstOpXmm;
            public const long InstOpMmXmmMem = InstOpMmXmm | InstOpMem;
            public const long InstOpXy       = InstOpXmm   | InstOpYmm;
            public const long InstOpXyMem    = InstOpXy    | InstOpMem;
            public const long InstOpXyz      = InstOpXy    | InstOpZmm;
            public const long InstOpXyzMem   = InstOpXyz   | InstOpMem;

            /// <summary>
            /// X86InstFlags
            /// </summary>
            public const long InstFlagNone        = 0x00000000;
            public const long InstFlagFlow        = 0x00000001;
            public const long InstFlagTest        = 0x00000002;
            public const long InstFlagMove        = 0x00000004;
            public const long InstFlagXchg        = 0x00000008;
            public const long InstFlagFp          = 0x00000010;
            public const long InstFlagLock        = 0x00000020;
            public const long InstFlagSpecial     = 0x00000040;
            public const long InstFlagSpecialMem  = 0x00000080;
            public const long InstFlagMem2        = 0x00000100;
            public const long InstFlagMem4        = 0x00000200;
            public const long InstFlagMem8        = 0x00000400;
            public const long InstFlagMem10       = 0x00000800;
            public const long InstFlagZ           = 0x00001000;
            public const long InstFlagAvx         = 0x00010000;
            public const long InstFlagXop         = 0x00020000;
            public const long InstFlagAvx512F     = 0x00100000;
            public const long InstFlagAvx512CD    = 0x00200000;
            public const long InstFlagAvx512PF    = 0x00400000;
            public const long InstFlagAvx512ER    = 0x00800000;
            public const long InstFlagAvx512DQ    = 0x01000000;
            public const long InstFlagAvx512BW    = 0x02000000;
            public const long InstFlagAvx512VL    = 0x04000000;
            public const long InstFlagAvx512KMask = 0x08000000;
            public const long InstFlagAvx512KZero = 0x10000000;
            public const long InstFlagAvx512BCast = 0x20000000;
            public const long InstFlagAvx512Sae   = 0x40000000;
            public const long InstFlagAvx512Rnd   = 0x80000000;

            /// <summary>
            /// X86InstFlagsInternal
            /// </summary>
            public const long InstFlagMem2_4    = InstFlagMem2   | InstFlagMem4;
            public const long InstFlagMem2_4_8  = InstFlagMem2_4 | InstFlagMem8;
            public const long InstFlagMem4_8    = InstFlagMem4   | InstFlagMem8;
            public const long InstFlagMem4_8_10 = InstFlagMem4_8 | InstFlagMem10;

            /// <summary>
            /// X86Cond
            /// </summary>
            public const long CondA   = 0x07;
            public const long CondAE  = 0x03;
            public const long CondB   = 0x02;
            public const long CondBE  = 0x06;
            public const long CondC   = 0x02;
            public const long CondE   = 0x04;
            public const long CondG   = 0x0F;
            public const long CondGE  = 0x0D;
            public const long CondL   = 0x0C;
            public const long CondLE  = 0x0E;
            public const long CondNA  = 0x06;
            public const long CondNAE = 0x02;
            public const long CondNB  = 0x03;
            public const long CondNBE = 0x07;
            public const long CondNC  = 0x03;
            public const long CondNE  = 0x05;
            public const long CondNG  = 0x0E;
            public const long CondNGE = 0x0C;
            public const long CondNL  = 0x0D;
            public const long CondNLE = 0x0F;
            public const long CondNO  = 0x01;
            public const long CondNP  = 0x0B;
            public const long CondNS  = 0x09;
            public const long CondNZ  = 0x05;
            public const long CondO   = 0x00;
            public const long CondP   = 0x0A;
            public const long CondPE  = 0x0A;
            public const long CondPO  = 0x0B;
            public const long CondS   = 0x08;
            public const long CondZ   = 0x04;
            public const long CondSign            = CondS;
            public const long CondNotSign         = CondNS;
            public const long CondOverflow        = CondO;
            public const long CondNotOverflow     = CondNO;
            public const long CondLess            = CondL;
            public const long CondLessEqual       = CondLE;
            public const long CondGreater         = CondG;
            public const long CondGreaterEqual    = CondGE;
            public const long CondBelow           = CondB;
            public const long CondBelowEqual      = CondBE;
            public const long CondAbove           = CondA;
            public const long CondAboveEqual      = CondAE;
            public const long CondEqual           = CondE;
            public const long CondNotEqual        = CondNE;
            public const long CondParityEven      = CondP;
            public const long CondParityOdd       = CondPO;
            public const long CondZero            = CondZ;
            public const long CondNotZero         = CondNZ;
            public const long CondNegative        = CondS;
            public const long CondPositive        = CondNS;
            public const long CondFpuUnordered    = 0x10;
            public const long CondFpuNotUnordered = 0x11;
            public const long CondNone            = 0x12;

            /// <summary>
            /// X86/X64 bytes used to encode important prefixes.
            /// </summary>
            public const byte ByteRex   = 0x40;
            public const byte ByteRexW  = 0x08;
            public const byte ByteVex2  = 0xC5;
            public const byte ByteVex3  = 0xC4;
            public const byte ByteXop3  = 0x8F;
            public const byte ByteEvex4 = 0x62;

            /// <summary>
            /// AsmJit specific (used to encode VVVV field in XOP/VEX).
            /// </summary>
            public const long VexVVVVShift = 12;
            public const long VexVVVVMask  = 0xF << (int)VexVVVVShift;

            /// <summary>
            /// X86InstOptions
            /// </summary>
            public const long InstOptionRex       = 0x00000040;
            public const long _InstOptionNoRex    = 0x00000080;
            public const long InstOptionLock      = 0x00000100;
            public const long InstOptionVex3      = 0x00000200;
            public const long InstOptionEvex      = 0x00010000;
            public const long InstOptionEvexZero  = 0x00020000;
            public const long InstOptionEvexOneN  = 0x00040000;
            public const long InstOptionEvexSae   = 0x00080000;
            public const long InstOptionEvexRnSae = 0x00100000;
            public const long InstOptionEvexRdSae = 0x00200000;
            public const long InstOptionEvexRuSae = 0x00400000;
            public const long InstOptionEvexRzSae = 0x00800000;
            public const long RexNoRexMask = InstOptionRex | _InstOptionNoRex;

            /// <summary>
            /// X86/X64 segment codes.
            /// </summary>
            public const long SegDefault = 0;
            public const long SegEs      = 1;
            public const long SegCs      = 2;
            public const long SegSs      = 3;
            public const long SegDs      = 4;
            public const long SegFs      = 5;
            public const long SegGs      = 6;
            public const long SegCount   = 7;
        }
    }
}