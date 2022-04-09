namespace AsmJit.Common.Operands
{
    public abstract class Variable : RegisterBase
    {
        protected Variable(VariableType type) : base(OperandType.Variable) => VariableType = type;

        protected Variable(Variable other) : base(other) { }

        internal VariableType VariableType
        {
            get => (VariableType)Reserved2;
            private set => Reserved2 = (int)value;
        }

        internal override T As<T>()
        {
            if (typeof(T) == typeof(Register)) return Register.FromVariable(this, Index) as T;

            return base.As<T>();
        }
    }
}