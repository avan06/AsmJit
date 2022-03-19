AsmJit
===========

x86/x64 JIT assembler for .NET

Managed .NET version of [asmjit](https://github.com/kobalicek/asmjit) by Petr Kobalicek.


### Features
-----

- Complete x86/x64 instruction set - MMX, SSEx, AVX1/2, BMI, XOP, FMA3, and FMA4 (AVX-512 in progress)
- Low-level and high-level code generation concepts
- Disassembly output (using [SharpDisasm](https://github.com/spazzarama/SharpDisasm))
- Zero dependencies (only couple of p/invoke calls for memory management)


### Code Generation Concepts
-----

AsmJit has two completely different code generation concepts. The difference is in how the code is generated. The first concept, also referred as a low level concept, is called Assembler and it's the same as writing RAW assembly by inserting instructions that use physical registers directly. In this case AsmJit does only instruction encoding, verification and final code relocation.

The second concept, also referred as a high level concept, is called Compiler. Compiler lets you use virtually unlimited number of registers (it calls them variables), which significantly simplifies the code generation process. Compiler allocates these virtual registers to physical registers after the code generation is done. This requires some extra effort - Compiler has to generate information for each node (instruction, function declaration, function call, etc...) in the code, perform a variable liveness analysis and translate the code using variables to a code that uses only physical registers.

In addition, Compiler understands functions and their calling conventions. It has been designed in a way that the code generated is always a function having a prototype like a real programming language. By having a function prototype the Compiler is able to insert prolog and epilog sequence to the function being generated and it's able to also generate a necessary code to call other function from your own code.

There is no conclusion on which concept is better. Assembler brings full control and the best performance, while Compiler makes the code-generation more fun and more portable.


### Quick start example1: Cpuid(high-level)
-----

```csharp
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
// eax = *r0
c.Emit(InstructionId.Mov, eax, X64 ? Memory.QWord(r0) : Memory.DWord(r0));
// ebx = *r1
c.Emit(InstructionId.Mov, ebx, X64 ? Memory.QWord(r1) : Memory.DWord(r1));
// ecx = *r2
c.Emit(InstructionId.Mov, ecx, X64 ? Memory.QWord(r2) : Memory.DWord(r2));
// edx = *r3
c.Emit(InstructionId.Mov, edx, X64 ? Memory.QWord(r3) : Memory.DWord(r3));

// Now execute Cpuid instruction
c.Emit(InstructionId.Cpuid, eax, ebx, ecx, edx);

// Load result back into arguments addresses
// *r0 = eax
c.Emit(InstructionId.Mov, X64 ? Memory.QWord(r0) : Memory.DWord(r0), eax);
// *r1 = ebx
c.Emit(InstructionId.Mov, X64 ? Memory.QWord(r1) : Memory.DWord(r1), ebx);
// *r2 = ecx
c.Emit(InstructionId.Mov, X64 ? Memory.QWord(r2) : Memory.DWord(r2), ecx);
// *r3 = eadx
c.Emit(InstructionId.Mov, X64 ? Memory.QWord(r3) : Memory.DWord(r3), edx);

// End return out of here :)
c.Ret();

// Now we tell Compiler's code context to generate shellcode and compile it into Action<IntPtr, IntPtr, IntPtr, IntPtr>
var cpuid = c.Compile();

// Now we can play around with generated delegate

// This array will hold our arguments.
var regs = new uint[4];

unsafe
{
    regs[0] = 1; // Let's get info about supported extenstions
    fixed (uint* pregs = regs)
    {
        // Call Cpuid
        cpuid((IntPtr)pregs, (IntPtr)(pregs + 1), (IntPtr)(pregs + 2), (IntPtr)(pregs + 3));
    }
}

// Now let's check if our CPU supports SSE2 instructions
var sse2bit = 1 << 25;
var isSse2Supported = (regs[2] & sse2bit) == sse2bit;
```


### Quick start example2: RawAssembler(low-level)
-----

```csharp
using R = AsmJit.AssemblerContext.Cpu.Registers;

// Create Compiler's (low-level) code context defining what kind of assembler we want it to be compiled to.
var c = Assembler.CreateContext<Func<int, int, int, int, int>>();

//The following Assembler implements the function of add up each parameters.
c.Emit(InstructionId.Push, R.Rbx);
c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
c.Emit(InstructionId.Mov, R.Ebx, R.Edx);
c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Eax, R.Ebx)); //lea ecx, [eax+ebx]
c.Emit(InstructionId.Lea, R.Edx, Memory.DWord(R.R8D, R.R9D)); //lea edx, [r8d+r9d]
c.Emit(InstructionId.Lea, R.Ecx, Memory.DWord(R.Ecx, R.Edx)); //lea ecx, [ecx+edx]
c.Emit(InstructionId.Mov, R.Eax, R.Ecx);
c.Emit(InstructionId.Pop, R.Rbx);
c.Ret();

var fn = c.Compile();
var result = fn(12, 24, 36, 48).ToString(); //120
```

More examples you can find in AsmJitTest/TestCases folder