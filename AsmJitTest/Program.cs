using System;
using System.Runtime.InteropServices;
using System.Security;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;
using AsmJitTest.TestCases;

namespace AsmJitTest
{
    class Program
    {
        private static bool _generateAsmListing = true;

        private static Action<IntPtr, IntPtr, UIntPtr> Make()
        {
            var c = Compiler.CreateContext<Action<IntPtr, IntPtr, UIntPtr>>();

            var dst = c.IntPtr("dst");
            var src = c.IntPtr("src");

            var i = c.IntPtr("i");
            var j = c.IntPtr("j");
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

            c.SetArgument(dst, src, i);

            c.Allocate(dst);
            c.Allocate(src);
            c.Allocate(i);

            // How many pixels have to be processed to make the loop aligned.
            c.Emit(InstructionId.Lea, t, Memory.Ptr(data));
            c.Emit(InstructionId.Xor, j, j);
            c.Emit(InstructionId.Xorps, cZero, cZero);

            c.Emit(InstructionId.Sub, j, dst);
            c.Emit(InstructionId.Movaps, cMul255A, Memory.Ptr(t, 0));

            c.Emit(InstructionId.And, j, (Immediate)15);
            c.Emit(InstructionId.Movaps, cMul255M, Memory.Ptr(t, 16));

            c.Emit(InstructionId.Shr, j, (Immediate)2);
            c.Emit(InstructionId.Jz, smallEnd);

            // j = min(i, j).
            c.Emit(InstructionId.Cmp, j, i);
            c.Emit(InstructionId.Cmovg, j, i);

            // i -= j.
            c.Emit(InstructionId.Sub, i, j);

            // Small loop.
            c.Bind(smallLoop);

            c.Emit(InstructionId.Pcmpeqb, a0, a0);
            c.Emit(InstructionId.Movd, y0, Memory.Ptr(src));

            c.Emit(InstructionId.Pxor, a0, y0);
            c.Emit(InstructionId.Movd, x0, Memory.Ptr(dst));

            c.Emit(InstructionId.Psrlw, a0, (Immediate)8);
            c.Emit(InstructionId.Punpcklbw, x0, cZero);

            c.Emit(InstructionId.Pshuflw, a0, a0, (Immediate)Utils.Shuffle(1, 1, 1, 1));
            c.Emit(InstructionId.Punpcklbw, y0, cZero);

            c.Emit(InstructionId.Pmullw, x0, a0);
            c.Emit(InstructionId.Paddsw, x0, cMul255A);
            c.Emit(InstructionId.Pmulhuw, x0, cMul255M);

            c.Emit(InstructionId.Paddw, x0, y0);
            c.Emit(InstructionId.Packuswb, x0, x0);

            c.Emit(InstructionId.Movd, Memory.Ptr(dst), x0);

            c.Emit(InstructionId.Add, dst, (Immediate)4);
            c.Emit(InstructionId.Add, src, (Immediate)4);

            c.Emit(InstructionId.Dec, j);
            c.Emit(InstructionId.Jnz, smallLoop);

            // Second section, prepare for an aligned loop.
            c.Bind(smallEnd);

            c.Emit(InstructionId.Test, i, i);
            c.Emit(InstructionId.Mov, j, i);
            c.Emit(InstructionId.Jz, c.Exit);

            c.Emit(InstructionId.And, j, (Immediate)3);
            c.Emit(InstructionId.Shr, i, (Immediate)2);
            c.Emit(InstructionId.Jz, largeEnd);

            // Aligned loop.
            c.Bind(largeLoop);

            c.Emit(InstructionId.Movups, y0, Memory.Ptr(src));
            c.Emit(InstructionId.Pcmpeqb, a0, a0);
            c.Emit(InstructionId.Movaps, x0, Memory.Ptr(dst));

            c.Emit(InstructionId.Xorps, a0, y0);
            c.Emit(InstructionId.Movaps, x1, x0);

            c.Emit(InstructionId.Psrlw, a0, (Immediate)8);
            c.Emit(InstructionId.Punpcklbw, x0, cZero);

            c.Emit(InstructionId.Movaps, a1, a0);
            c.Emit(InstructionId.Punpcklwd, a0, a0);

            c.Emit(InstructionId.Punpckhbw, x1, cZero);
            c.Emit(InstructionId.Punpckhwd, a1, a1);

            c.Emit(InstructionId.Pshufd, a0, a0, (Immediate)Utils.Shuffle(3, 3, 1, 1));
            c.Emit(InstructionId.Pshufd, a1, a1, (Immediate)Utils.Shuffle(3, 3, 1, 1));

            c.Emit(InstructionId.Pmullw, x0, a0);
            c.Emit(InstructionId.Pmullw, x1, a1);

            c.Emit(InstructionId.Paddsw, x0, cMul255A);
            c.Emit(InstructionId.Paddsw, x1, cMul255A);

            c.Emit(InstructionId.Pmulhuw, x0, cMul255M);
            c.Emit(InstructionId.Pmulhuw, x1, cMul255M);

            c.Emit(InstructionId.Add, src, (Immediate)16);
            c.Emit(InstructionId.Packuswb, x0, x1);

            c.Emit(InstructionId.Paddw, x0, y0);
            c.Emit(InstructionId.Movaps, Memory.Ptr(dst), x0);

            c.Emit(InstructionId.Add, dst, (Immediate)16);

            c.Emit(InstructionId.Dec, i);
            c.Emit(InstructionId.Jnz, largeLoop);

            c.Bind(largeEnd);
            c.Emit(InstructionId.Test, j, j);
            c.Emit(InstructionId.Jnz, smallLoop);

            // Data
            c.Data(data, 16,
                Data.Of(0x0080008000800080, 0x0080008000800080),
                Data.Of(0x0101010101010101, 0x0080008000800080));

            return c.Compile();
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

        private static void RunBlendSrcOverTest()
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

            var t1 = new ExecutionTimer();
            var t2 = new ExecutionTimer();

            #region NetVerTest
            t1.Start();
            for (int i = 0; i < 1000000; i++)
            {
                var e = new uint[cnt];
                for (var z = 0; z < cnt; z++)
                {
                    e[z] = BlendSrcOver(dst[z], src[z]);
                }	
            }
            t1.Stop();
            var tt1 = t1.Milliseconds;
            #endregion

            #region AsmVerTest
            var fn = Make();

            t2.Start();
            for (int i = 0; i < 1000000; i++)
            {
                fn(pdst, psrc, (UIntPtr)cnt);
            }
            t2.Stop();
            var tt2 = t2.Milliseconds;
            #endregion

            Console.WriteLine("BlendSrcOver Test, ");
            Console.WriteLine(string.Format("NET ver elapsed: {0} Milliseconds", tt1));
            Console.WriteLine(string.Format("Asm ver elapsed: {0} Milliseconds", tt2));
            Console.ReadKey();

            UnsafeMemory.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), pdst, cnt * sizeof(uint));
        }

        static void Main(string[] arguments)
        {
            var cpuid = Cpuid.Get();

            Console.WriteLine("CPU:\t" + cpuid.Cpu);
            Console.WriteLine();
            Console.WriteLine("\tCache size:\t\t" + cpuid.CacheSize);
            Console.WriteLine("\tCache line size:\t" + cpuid.CacheLineSize);
            Console.WriteLine();
            Console.WriteLine("Extensions: ");
            Console.WriteLine("\tSSE:\t" + (cpuid.Sse ? "Yes" : "No"));
            Console.WriteLine("\tSSE2:\t" + (cpuid.Sse2 ? "Yes" : "No"));
            Console.WriteLine("\tSSE3:\t" + (cpuid.Sse3 ? "Yes" : "No"));
            Console.WriteLine("\tSSE4.1:\t" + (cpuid.Sse41 ? "Yes" : "No"));
            Console.WriteLine("\tSSE4.2:\t" + (cpuid.Sse42 ? "Yes" : "No"));
            Console.WriteLine("\tAVX:\t" + (cpuid.Avx ? "Yes" : "No"));
            Console.WriteLine("\tAVX2:\t" + (cpuid.Avx2 ? "Yes" : "No"));
            Console.WriteLine();

            var tr = new TestCaseRunner(_generateAsmListing);

            tr.Add(new JumpCross());
            tr.Add(new JumpUnreachable1());
            tr.Add(new JumpUnreachable2());
            tr.Add(new AllocBase());
            tr.Add(new AllocManual());
            tr.Add(new AllocUseMem());
            tr.Add(new AllocMany1());
            tr.Add(new AllocMany2());
            tr.Add(new AllocImul1());
            tr.Add(new AllocImul2());
            tr.Add(new AllocIdiv1());
            tr.Add(new AllocSetz());
            tr.Add(new AllocShlRor());
            tr.Add(new AllocGpLo());
            tr.Add(new AllocRepMovsb());
            tr.Add(new AllocIfElse1());
            tr.Add(new AllocIfElse2());
            tr.Add(new AllocIfElse3());
            tr.Add(new AllocIfElse4());
            tr.Add(new AllocInt8());
            tr.Add(new AllocArgsIntPtr());
            tr.Add(new AllocArgsFloat());
            tr.Add(new AllocArgsDouble());
            tr.Add(new AllocRetFloat());
            tr.Add(new AllocRetDouble());
            tr.Add(new AllocStack());
            tr.Add(new AllocMemcpy());
            tr.Add(new AllocBlend());
            tr.Add(new CallBase());
            tr.Add(new CallFast());
            tr.Add(new CallManyArgs());
            tr.Add(new CallDuplicateArgs());
            tr.Add(new CallImmArgs());
            tr.Add(new CallPtrArgs());
            tr.Add(new CallFloatAsXmmRet());
            tr.Add(new CallDoubleAsXmmRet());
            tr.Add(new CallConditional());
            tr.Add(new CallMultiple());
            tr.Add(new CallRecursive());
            tr.Add(new CallMisc1());
            tr.Add(new CallMisc2());
            tr.Add(new CallMisc3());
            tr.Add(new CallMisc4());
            tr.Add(new MiscConstPool());
            tr.Add(new MiscMultiRet());

            tr.Add(new RawAssembler());

            var resTrue = 0;
            var resFalse = 0;

            foreach (var data in tr.Run())
            {
                bool result = data.result;
                Console.WriteLine(string.Format("{0}:{1}", data.name, result));
                if (result) resTrue++;
                else resFalse++;

                if (!_generateAsmListing) continue;

                string asm = data.disasm;
                Console.WriteLine(asm);
            }
            Console.WriteLine();
            Console.WriteLine("Passed:\t" + resTrue);
            Console.WriteLine("Failed:\t" + resFalse);
            Console.ReadKey();
            Console.WriteLine();

            RunBlendSrcOverTest();
        }
    }

    public class ExecutionTimer
    {

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", EntryPoint = "QueryPerformanceCounter")]
        public static extern uint QueryPerformanceCounter(ref long t);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", EntryPoint = "QueryPerformanceFrequency")]
        public static extern uint QueryPerformanceFrequency(ref long t);

        private long _startTime;
        private long _stopTime;
        private long _freq;

        public ExecutionTimer()
        {
            _startTime = 0;
            _stopTime = 0;
        }

        public void Start()
        {
            QueryPerformanceFrequency(ref _freq);
            QueryPerformanceCounter(ref _startTime);
        }

        public void Stop()
        {
            QueryPerformanceCounter(ref _stopTime);
        }

        public double Milliseconds => ((_stopTime - _startTime) * 1000.0) / _freq;
    }
}
