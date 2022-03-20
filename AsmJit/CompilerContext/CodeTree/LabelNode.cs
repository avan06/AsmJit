namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class LabelNode : CodeNode
    {
        public readonly int LabelId;

        public int ReferenceCount;

        public JumpNode From;

        public LabelNode(int labelId) : base(CodeNodeType.Label) => LabelId = labelId;

        public override string ToString() => string.Format("[{0}] {1}: Id={2}, From=({3})", FlowId == 0 ? "#" : FlowId.ToString(), Type, LabelId, From);
    }
}