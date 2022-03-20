using AsmJit.Common.Enums;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class AlignNode : CodeNode
    {
        public readonly AligningMode AlignMode;

        public readonly int Offset;

        public AlignNode(AligningMode alignMode, int offset) : base(CodeNodeType.Alignment)
        {
            AlignMode = alignMode;
            Offset = offset;
        }
    }
}