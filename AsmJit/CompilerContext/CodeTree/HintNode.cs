using AsmJit.Common.Variables;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class HintNode : CodeNode
    {
        public readonly VariableData Data;

        public readonly VariableHint Hint;

        public readonly int Value;

        public HintNode(VariableData data, VariableHint hint, int value) : base(CodeNodeType.Hint)
        {
            Data = data;
            Hint = hint;
            Value = value;
            Flags |= (CodeNodeFlags.Removable | CodeNodeFlags.Informative);
        }

        public override string ToString() => string.Format("[{0}] {1}: Hint={2}, Value={3}", FlowId == 0 ? "#" : FlowId.ToString(), Type, Hint, Value);
    }
}