using AsmJit.Common.Extensions;
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

        internal bool IsFetched() => FlowId != 0;

        internal bool IsRemovable() => Flags.IsSet(CodeNodeFlags.Removable);

        internal bool IsInformative() => Flags.IsSet(CodeNodeFlags.Informative);

        internal bool IsTranslated() => Flags.IsSet(CodeNodeFlags.Translated);

        internal bool IsScheduled() => Flags.IsSet(CodeNodeFlags.Scheduled);

        internal bool IsRet() => Flags.IsSet(CodeNodeFlags.Ret);

        internal bool IsJmp() => Flags.IsSet(CodeNodeFlags.Jmp);

        internal bool IsJcc() => Flags.IsSet(CodeNodeFlags.Jcc);

        internal bool IsJmpOrJcc() => IsJmp() || IsJcc();
    }
}
