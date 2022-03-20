namespace AsmJit.Common
{
    internal class LabelLink
    {
        internal LabelLink Previous;

        internal int Offset;

        internal int Displacement;

        internal int RelocationId;

        internal LabelLink()
        {
            Offset = Constants.InvalidValue;
            RelocationId = Constants.InvalidId;
        }
    }
}