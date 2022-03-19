using AsmJit.Common.Operands;
using AsmJit.CompilerContext;

namespace AsmJit.AssemblerContext
{
    internal class SpecialInstruction
    {
        public SpecialInstruction(int outReg, VariableFlags flags) : this(RegisterIndex.Invalid, outReg, flags) { }
        public SpecialInstruction(VariableFlags flags) : this(RegisterIndex.Invalid, RegisterIndex.Invalid, flags) { }
        public SpecialInstruction(int inReg, int outReg, VariableFlags flags)
        {
            InReg = inReg;
            OutReg = outReg;
            Flags = flags;
        }

        public int InReg { get; private set; }

        public int OutReg { get; private set; }

        public VariableFlags Flags { get; private set; }
    }
}