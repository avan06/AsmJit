using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AsmJit.AssemblerContext;
using AsmJit.CompilerContext;
using SharpDisasm;
using CodeContext = AsmJit.CompilerContext.CodeContext;

namespace AsmJitTest
{
    public sealed class TestCaseRunner
    {
        private bool _disasm;
        private List<TestCase> _testCases = new List<TestCase>();

        public TestCaseRunner(bool generateAsmListing = false) => _disasm = generateAsmListing;

        public void Add(TestCase testCase) => _testCases.Add(testCase);

        public IEnumerable<(string name, bool result, string value, string expected, string disasm)> Run()
        {
            foreach (var testCase in _testCases)
            {
                bool res = false;
                string value = null, expected = null, disasm = null;
                try { res = _disasm ? testCase.Run(out value, out expected, out disasm) : testCase.Run(out value, out expected); }
                catch (Exception ex) { Console.WriteLine(ex.Message + "\n" + ex.StackTrace); }
                yield return (testCase.GetType().Name, res, value, expected, disasm);
            }
        }
    }

    public abstract class TestCase
    {
        public abstract bool Run(out string value, out string expected);
        public abstract bool Run(out string value, out string expected, out string disassemly);
    }

    public abstract class CompilerTestCase<T> : TestCase
    {
        private AsmJit.CompilerContext.CodeContext<T> _ctx;

        protected CompilerTestCase() => _ctx = Compiler.CreateContext<T>();

        public override bool Run(out string value, out string expected)
        {
            Compile(_ctx);
            Execute(_ctx.Compile(), out value, out expected);
            return value == expected;
        }

        public override unsafe bool Run(out string value, out string expected, out string disassemly)
        {
            Compile(_ctx);
            Execute(_ctx.Compile(out IntPtr fp, out int codeSize), out value, out expected);
            var tmp = Marshal.AllocHGlobal(codeSize);
            Buffer.MemoryCopy((void*)fp, (void*)tmp, codeSize, codeSize);
            const ArchitectureMode mode = ArchitectureMode.x86_64;
            Disassembler.Translator.IncludeAddress = true;
            Disassembler.Translator.IncludeBinary = true;
            var disasm = new Disassembler(tmp, codeSize, mode, (ulong)fp.ToInt64(), true);
            disassemly = disasm.Disassemble().Aggregate("", (current, insn) => current + insn + Environment.NewLine);
            Marshal.FreeHGlobal(tmp);
            return value == expected;
        }

        protected abstract void Compile(CodeContext c);
        protected abstract void Execute(T fn, out string result, out string expected);
    }

    public abstract class AssemblerTestCase<T> : TestCase
    {
        private AsmJit.AssemblerContext.CodeContext<T> _ctx;

        protected AssemblerTestCase() => _ctx = Assembler.CreateContext<T>();

        public override bool Run(out string value, out string expected)
        {
            Compile(_ctx);
            Execute(_ctx.Compile(), out value, out expected);
            return value == expected;
        }

        public override unsafe bool Run(out string value, out string expected, out string disassemly)
        {
            Compile(_ctx);
            Execute(_ctx.Compile(out IntPtr fp, out int codeSize), out value, out expected);
            var tmp = Marshal.AllocHGlobal(codeSize);
            Buffer.MemoryCopy((void*)fp, (void*)tmp, codeSize, codeSize);
            //
            //byte[] managedArray = new byte[codeSize];
            //Marshal.Copy(fp, managedArray, 0, codeSize);
            //for (int idx = 0; idx < managedArray.Length; idx++)
            //{
            //    if (idx > 0 && idx % 8 == 0) Console.WriteLine();
            //    Console.Write(managedArray[idx].ToString("X") + " ");
            //}
            //
            const ArchitectureMode mode = ArchitectureMode.x86_64;
            Disassembler.Translator.IncludeAddress = true;
            Disassembler.Translator.IncludeBinary = true;
            var disasm = new Disassembler(tmp, codeSize, mode, (ulong)fp.ToInt64(), true);
            disassemly = disasm.Disassemble().Aggregate("", (current, insn) => current + insn + Environment.NewLine);
            Marshal.FreeHGlobal(tmp);
            return value == expected;
        }

        protected abstract void Compile(AsmJit.AssemblerContext.CodeContext c);
        protected abstract void Execute(T fn, out string result, out string expected);
    }
}