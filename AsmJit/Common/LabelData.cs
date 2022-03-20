namespace AsmJit.Common
{
    internal class LabelData
    {
        public int Offset;

        public LabelLink Links;

        public readonly int ContextId;

        public LabelData(int contextId)
        {
            ContextId = contextId;
            Offset = Constants.InvalidValue;
        }
    }
}
