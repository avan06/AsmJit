using AsmJit.Common.Operands;
using AsmJit.Common.Variables;

namespace AsmJit.Common
{
    internal class SpecialInstruction
    {
        public readonly int InReg;

        public readonly int OutReg;

        public readonly VariableFlags Flags;

        public SpecialInstruction(int outReg, VariableFlags flags) : this(RegisterIndex.Invalid, outReg, flags) { }
        public SpecialInstruction(VariableFlags flags) : this(RegisterIndex.Invalid, RegisterIndex.Invalid, flags) { }
        public SpecialInstruction(int inReg, int outReg, VariableFlags flags)
        {
            InReg = inReg;
            OutReg = outReg;
            Flags = flags;
        }
    }
}