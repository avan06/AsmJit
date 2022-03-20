using AsmJit.Common;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class DataNode : CodeNode
    {
        public readonly Pointer Data;

        public readonly int Size;

        public DataNode(Pointer data, int size) : base(CodeNodeType.Data)
        {
            Size = size;
            Data = data;
        }
    }
}