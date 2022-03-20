using System.Linq;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext.CodeTree
{
    internal class InstructionNode : CodeNode
    {
        public readonly InstructionId InstructionId;

        public readonly InstructionOptions InstructionOptions;

        public readonly Operand[] Operands;

        public int MemoryOperandIndex;

        public InstructionNode(InstructionId instructionId, InstructionOptions instructionOptions, Operand[] operands) : base(CodeNodeType.Instruction)
        {
            InstructionId = instructionId;
            InstructionOptions = instructionOptions;
            Operands = operands;
            MemoryOperandIndex = Constants.InvalidValue;
            for (var i = 0; i < operands.Length; i++)
            {
                if (!operands[i].IsMemory()) continue;
                MemoryOperandIndex = i;
                break;
            }
            Flags |= CodeNodeFlags.Removable;
        }

        public override string ToString() => string.Format("[{0}] {1}: {2}, Ops={3}", FlowId == 0 ? "#" : FlowId.ToString(), Type, InstructionId, string.Join(", ", Operands.ToList().Select(o => o.ToString())));
    }
}