using AsmJit.Common.Enums;
using AsmJit.Common.Operands;

namespace AsmJit.Common.Variables
{
    internal class VariableInfo
    {
        public readonly RegisterType RegisterType;

        public readonly int Size;

        public readonly RegisterClass RegisterClass;

        public readonly VariableValueFlags ValueFlags;

        public readonly string Name;

        public VariableInfo(RegisterType registerType, int size, RegisterClass registerClass, VariableValueFlags valueFlags, string name)
        {
            RegisterType = registerType;
            Size = size;
            RegisterClass = registerClass;
            ValueFlags = valueFlags;
            Name = name;
        }
    }
}