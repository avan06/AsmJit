using AsmJit.Common.Operands;

namespace AsmJit.Common
{
    public class DataBlock
    {
        public readonly Data[] Data;

        public readonly int Alignment;

        public readonly Label Label;

        public DataBlock(Label label, int alignment, params Data[] data)
        {
            Label = label;
            Data = data;
            Alignment = alignment;
        }
    }
}