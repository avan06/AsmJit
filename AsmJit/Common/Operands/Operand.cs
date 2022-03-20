namespace AsmJit.Common.Operands
{
    public abstract class Operand
    {
        public static readonly Invalid Invalid = new Invalid();
        protected internal readonly OperandType OperandType;

        protected Operand(OperandType type)
        {
            Id = Constants.InvalidId;
            OperandType = type;
        }

        protected Operand(OperandType type, int size)
        {
            Id = Constants.InvalidId;
            OperandType = type;
            Size = size;
        }

        protected Operand(Operand other)
        {
            OperandType = other.OperandType;
            Id = other.Id;
            Reserved0 = other.Reserved0;
            Reserved1 = other.Reserved1;
            Size = other.Size;
            Reserved2 = other.Reserved2;
            Reserved3 = other.Reserved3;
        }

        protected internal int Size { get; protected set; }

        protected internal int Reserved0 { get; protected set; }

        protected internal int Reserved1 { get; protected set; }

        protected internal int Id { get; protected set; }

        protected internal int Reserved2 { get; protected set; }

        protected internal int Reserved3 { get; protected set; }

        internal virtual T As<T>() where T : Operand => this as T;

        public override string ToString() => string.Format("[{0}: Id={1}, Size={2}]", OperandType, Id == Constants.InvalidId ? "#" : Id.ToString(), Size);
    }
}
