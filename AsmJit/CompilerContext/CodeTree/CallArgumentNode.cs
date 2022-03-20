using AsmJit.Common.Variables;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class CallArgumentNode : CodeNode
    {
        public readonly CallNode Call;

        public readonly VariableData Source;

        public readonly VariableData Conv;

        public int AffectedArguments;

        public CallArgumentNode(CallNode call, VariableData src, VariableData conv) : base(CodeNodeType.CallArgument)
        {
            Call = call;
            Source = src;
            Conv = conv;
        }
    }
}