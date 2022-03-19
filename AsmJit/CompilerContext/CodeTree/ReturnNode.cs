using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class ReturnNode : CodeNode
    {
        public ReturnNode(Operand o0 = null, Operand o1 = null) : base(CodeNodeType.Return)
        {
            if (o0 == null) o0 = Operand.Invalid;
            if (o1 == null) o1 = Operand.Invalid;
            Operands = new Operand[2];
            Operands[0] = o0;
            Operands[1] = o1;
            Flags |= CodeNodeFlags.Ret;
        }

        public Operand[] Operands { get; private set; }
    }
}