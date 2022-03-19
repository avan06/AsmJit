using System;
using System.Runtime.InteropServices;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJitTest.TestCases
{
    public sealed class CallMisc4 : CompilerTestCase<Func<int, int, int>>
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public static extern unsafe IntPtr LoadLibrary([In][MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        public static extern unsafe IntPtr GetProcAddress([In] IntPtr hModule, [In][MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        protected override void Compile(CodeContext c)
        {
            var h = LoadLibrary(IntPtr.Size > 4 ? "SampleDLL64.dll" : "SampleDLL.dll");
            var fnPtr = GetProcAddress(h, "addNumbers");

            var fp = Memory.Fn<Func<int, int, int>>(fnPtr);
            var fn = c.IntPtr("fn");
            c.Emit(InstructionId.Mov, fn, fp);
            var call = c.Call(fn, fp);
            call.SetArgument(c.Int32("v0"), c.Int32("v1"));
            var v2 = call.SetReturn(c.Int32("v2"));

            c.Ret(v2);
        }

        protected override void Execute(Func<int, int, int> fn, out string result, out string expected)
        {
            result = fn(4, 5).ToString();
            expected = 9.ToString();
        }
    }
}