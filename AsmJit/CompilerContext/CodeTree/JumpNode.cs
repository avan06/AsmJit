using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class JumpNode : InstructionNode
    {
        public LabelNode Target;

        public JumpNode NextJump;

        public JumpNode(InstInfo instructionId, InstructionOptions instructionOptions, Operand[] operands) : base(instructionId, instructionOptions, operands) { }
    }
}