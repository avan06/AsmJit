using AsmJit.Common.Variables;

namespace AsmJit.CompilerContext.CodeTree
{
    public abstract class CodeNode
    {
        public CodeNode Previous;

        public CodeNode Next;

        internal readonly CodeNodeType Type;

        internal CodeNodeFlags Flags;

        public int FlowId;

        internal VariableMap VariableMap;

        internal VariableState VariableState;

        internal BitArray Liveness;

        internal CodeNode(CodeNodeType type)
        {
            Flags |= CodeNodeFlags.Removable;
            Type = type;
        }

        public T As<T>() where T : CodeNode => this as T;
    }
}
