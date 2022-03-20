using System;
using AsmJit.Common.Operands;

namespace AsmJit.Common
{
    internal class FunctionInOut
    {
        public readonly VariableType VariableType;

        public readonly int RegisterIndex;

        public readonly int StackOffset;

        public FunctionInOut(VariableType variableType, int registerIndex, int stackOffset = Constants.InvalidValue)
        {
            VariableType = variableType;
            RegisterIndex = registerIndex;
            StackOffset = stackOffset;
        }
    }
}
