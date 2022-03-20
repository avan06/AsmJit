using System;
using System.Text;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest
{
    public sealed class Cpuid
    {
        enum Register
        {
            Eax = 0,
            Ebx = 1,
            Ecx = 2,
            Edx = 3,
        };

        enum Bit
        {
            // Ordinary:
            // Edx:
            SSE = 1 << 25,
            SSE2 = 1 << 26,

            // Ecx:
            SSSE3 = 1 << 9,
            SSE41 = 1 << 19,
            SSE42 = 1 << 20,
            OSXSAVE = 1 << 27,
            AVX = 1 << 28,

            // Extended:
            // Ebx:
            AVX2 = 1 << 5,
            AVX512F = 1 << 16,
            AVX512BW = 1 << 30,

            // Ecx:
            AVX512VBMI = 1 << 1,
        }

        static bool CheckBit(uint[] registers, Register index, Bit bit) => (registers[(int)index] & (int)bit) == (int)bit;
        
        private static readonly bool X64 = IntPtr.Size > 4;

        private static Cpuid _instance;

        private Cpuid() { }

        public static Cpuid Get()
        {
            if (_instance != null) return _instance;

            _instance = new Cpuid();

            // Create Compiler's (high-level) code context defining what kind of function (delegate) we want it to be compiled to.
            // We use IntPtr here as ref/out functionality replacament to get several return values
            var c = Compiler.CreateContext<Action<IntPtr, IntPtr, IntPtr, IntPtr>>();

            // Define some temp variables. The names are just the names in this case.
            var eax = X64 ? c.UInt64() : c.UInt32();
            var ebx = X64 ? c.UInt64() : c.UInt32();
            var ecx = X64 ? c.UInt64() : c.UInt32();
            var edx = X64 ? c.UInt64() : c.UInt32();

            // Define variables for our function arguments.
            var r0 = X64 ? c.UInt64() : c.UInt32();
            var r1 = X64 ? c.UInt64() : c.UInt32();
            var r2 = X64 ? c.UInt64() : c.UInt32();
            var r3 = X64 ? c.UInt64() : c.UInt32();

            // Assign each argument to its variable
            c.SetArgument(r0, r1, r2, r3);

            // Load values arguments point to (dereference IntPtr)
            c.Emit(
                // eax = *r0
                InstructionId.Mov, eax, X64 ? Memory.QWord(r0) : Memory.DWord(r0),
                // ebx = *r1
                InstructionId.Mov, ebx, X64 ? Memory.QWord(r1) : Memory.DWord(r1),
                // ecx = *r2
                InstructionId.Mov, ecx, X64 ? Memory.QWord(r2) : Memory.DWord(r2),
                // edx = *r3
                InstructionId.Mov, edx, X64 ? Memory.QWord(r3) : Memory.DWord(r3),

                // Now execute Cpuid instruction
                InstructionId.Cpuid, eax, ebx, ecx, edx,

                // Load result back into arguments addresses
                // *r0 = eax
                InstructionId.Mov, X64 ? Memory.QWord(r0) : Memory.DWord(r0), eax,
                // *r1 = ebx
                InstructionId.Mov, X64 ? Memory.QWord(r1) : Memory.DWord(r1), ebx,
                // *r2 = ecx
                InstructionId.Mov, X64 ? Memory.QWord(r2) : Memory.DWord(r2), ecx,
                // *r3 = eadx
                InstructionId.Mov, X64 ? Memory.QWord(r3) : Memory.DWord(r3), edx);

            // End return out of here :)
            c.Ret();

            // Now we tell Compiler's code context to generate shellcode and compile it into Action<IntPtr, IntPtr, IntPtr, IntPtr>
            var fn = c.Compile();

            // Now we can play around with generated delegate

            // This array will hold our arguments.
            var regs = new uint[4];

            unsafe
            {
                regs[0] = 1; // Let's get info about supported extenstions
                fixed (uint* pregs = regs)
                {
                    // Call Cpuid
                    fn((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
                }
            }

            // Now let's check if our CPU supports SSE2 instructions
            _instance.Sse = CheckBit(regs, Register.Edx, Bit.SSE);
            _instance.Sse2 = CheckBit(regs, Register.Edx, Bit.SSE2);
            _instance.Sse3 = CheckBit(regs, Register.Ecx, Bit.SSSE3);
            _instance.Sse41 = CheckBit(regs, Register.Ecx, Bit.SSE41);
            _instance.Sse42 = CheckBit(regs, Register.Ecx, Bit.SSE42);
            _instance.Avx = CheckBit(regs, Register.Ecx, Bit.OSXSAVE) && CheckBit(regs, Register.Ecx, Bit.AVX);
            _instance.Avx2 = CheckBit(regs, Register.Ecx, Bit.OSXSAVE) && CheckBit(regs, Register.Ecx, Bit.AVX2);

            regs = new uint[4];
            unsafe
            {
                regs[0] = 0x80000002;
                fixed (uint* pregs = regs)
                {
                    fn((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
                }
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[0]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[1]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[2]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[3]));
                regs[0] = 0x80000003;
                fixed (uint* pregs = regs)
                {
                    fn((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
                }
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[0]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[1]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[2]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[3]));
                regs[0] = 0x80000004;
                fixed (uint* pregs = regs)
                {
                    fn((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
                }
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[0]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[1]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[2]));
                _instance.Cpu += Encoding.Default.GetString(BitConverter.GetBytes(regs[3]));
                _instance.Cpu = _instance.Cpu.Replace('\0', ' ').Trim();
                regs[0] = 0x80000006;
                fixed (uint* pregs = regs)
                {
                    fn((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
                }
                _instance.CacheLineSize = (int)(regs[2] & 0xff);
                _instance.CacheSize = (int)((regs[2] >> 16) & 0xffff);
            }

            return _instance;
        }

        public string Cpu { get; private set; }
        
        public int CacheLineSize { get; private set; }

        public int CacheSize { get; private set; }

        public bool Sse { get; private set; }

        public bool Sse2 { get; private set; }
        
        public bool Sse3 { get; private set; }

        public bool Sse41 { get; private set; }
        
        public bool Sse42 { get; private set; }
        
        public bool Avx { get; private set; }
        
        public bool Avx2 { get; private set; }
    }
}