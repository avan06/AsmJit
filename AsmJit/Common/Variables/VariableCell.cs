namespace AsmJit.Common.Variables
{
    internal class VariableCell
    {
        public VariableCell Next;

        public int Offset;

        public int Size;

        public int Alignment;
    }
}