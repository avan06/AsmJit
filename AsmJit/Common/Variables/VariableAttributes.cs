using AsmJit.Common.Operands;

namespace AsmJit.Common.Variables
{
    internal class VariableAttributes
    {
        public VariableData VariableData;

        public VariableFlags Flags;

        public int UsageCount;

        public int InRegIndex;

        public int OutRegIndex;

        public int InRegs;

        public int AllocableRegs;

        public void Setup(VariableData vd, VariableFlags flags = 0, int inRegs = 0, int allocableRegs = 0)
        {
            VariableData = vd;
            Flags = flags;
            UsageCount = 0;
            InRegIndex = RegisterIndex.Invalid;
            OutRegIndex = RegisterIndex.Invalid;
            InRegs = inRegs;
            AllocableRegs = allocableRegs;
        }

        public void CopyFrom(VariableAttributes va)
        {
            VariableData = va.VariableData;
            Flags = va.Flags;
            UsageCount = va.UsageCount;
            InRegIndex = va.InRegIndex;
            OutRegIndex = va.OutRegIndex;
            InRegs = va.InRegs;
            AllocableRegs = va.AllocableRegs;
        }
    }
}