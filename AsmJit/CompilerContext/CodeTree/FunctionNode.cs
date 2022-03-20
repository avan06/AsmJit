using System;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.Common.Variables;

namespace AsmJit.CompilerContext.CodeTree
{
    internal sealed class FunctionNode : CodeNode
    {
        private VariableData[] _arguments;
        private int _hints;

        public readonly LabelNode Entry;

        public readonly LabelNode Exit;

        public readonly SentinelNode End;

        public readonly FunctionDeclaration FunctionDeclaration;

        public readonly RegisterMask SaveRestoreRegs;

        public readonly int[] StackFrameCopyGpIndex;

        public FunctionNodeFlags FunctionFlags;
        //public FuncionNodeHints Hints;

        public int AlignStackSize;

        public int AlignedMemStackSize;

        public int PushPopStackSize;

        public int MoveStackSize;

        public int ExtraStackSize;

        public int StackFrameRegIndex;

        public bool IsStackFrameRegPreserved;

        public int RequiredStackAlignment;

        public int CallStackSize;

        public int MemStackSize;

        public FunctionNode(LabelNode entry, LabelNode exit, VariableType[] arguments, VariableType @return, CallingConvention callingConvention) : base(CodeNodeType.Function)
        {
            Entry = entry;
            Exit = exit;
            End = new SentinelNode();
            FunctionDeclaration = new FunctionDeclaration(callingConvention, arguments, @return);
            _arguments = new VariableData[arguments.Length];
            StackFrameCopyGpIndex = new int[6].InitializeWith(() => RegisterIndex.Invalid);
            SaveRestoreRegs = new RegisterMask();
            StackFrameRegIndex = RegisterIndex.Invalid;
            _hints = Utils.Mask((int)FuncionNodeHints.Naked);
        }
        // TODO
        public VariableData GetArgument(int index)
        {
            if (index < 0 || index >= _arguments.Length) throw new ArgumentException(string.Format("invalid argument index:{0}", index));
            return _arguments[index];
        }

        public void SetArgument(int index, VariableData value)
        {
            if (index < 0 || index >= _arguments.Length) throw new ArgumentException(string.Format("invalid argument index:{0}", index));
            _arguments[index] = value;
        }

        public void UpdateRequiredStackAlignment()
        {
            if (RequiredStackAlignment <= Cpu.Info.StackAlignment)
            {
                RequiredStackAlignment = Cpu.Info.StackAlignment;
                FunctionFlags &= ~FunctionNodeFlags.IsStackMisaligned;
            }
            else FunctionFlags |= FunctionNodeFlags.IsStackMisaligned;
        }

        public void MergeCallStackSize(int s)
        {
            if (CallStackSize < s) CallStackSize = s;
        }

        public bool HasHint(FuncionNodeHints hint) => ((_hints >> (int)hint) & 0x1) != 0;

        public void SetHint(FuncionNodeHints hint, int value)
        {
            _hints &= ~(1 << (int)hint);
            _hints |= value << (int)hint;
        }
    }
}