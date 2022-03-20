using System;
using AsmJit.Common.Operands;

namespace AsmJit.Common.Variables
{
    internal class VariableData
    {
        public readonly VariableType Type;

        public readonly VariableInfo Info;

        public readonly int Id;

        public readonly string Name;

        public readonly bool IsCalculated;

        public readonly bool SaveOnUnuse;

        public readonly bool IsStack;

        public readonly int Alignment;

        public int LocalId;

        public int RegisterIndex;

        public int Priority;

        public VariableAttributes Attributes;

        public bool IsMemoryArgument;

        public bool IsModified;

        public int HomeMask;

        public int MemoryOffset;

        public VariableData(VariableType type, VariableInfo info, int id, string name = null, int alignment = 0)
        {
            Type = type;
            Info = info;
            Id = id;
            Name = name;
            LocalId = Constants.InvalidId;
            RegisterIndex = Operands.RegisterIndex.Invalid;
            State = VariableUsage.None;
            Priority = 10;
            Alignment = alignment == 0 ? Math.Min(info.Size, 64) : Math.Min(alignment, 64);
            if (type != VariableType.Stack) return;
            IsStack = true;
            Type = VariableType.Invalid;
        }

        public VariableUsage State { get; internal set; }

        public VariableCell MemoryCell { get; internal set; }

        public override string ToString() => string.Format("{0} [{1}]:{2}", string.IsNullOrEmpty(Name) ? "NA" : Name, Type, RegisterIndex);
    }
}