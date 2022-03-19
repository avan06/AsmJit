using System;
using AsmJit.Common;
using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext.CodeTree
{
    public class CallNode : CodeNode
    {
        private int currArgLen;
        private int currRetLen;
        private RegisterMask _usedArgs;

        internal CallNode(Operand target, FunctionDeclaration decl) : base(CodeNodeType.Call)
        {
            currArgLen = 0;
            currRetLen = 0;
            Target = target;
            FunctionDeclaration = decl;
            Arguments = new Operand[decl.ArgumentCount];
            Return = new Operand[2].InitializeWith(() => Operand.Invalid);
        }
        public T SetArgument<T>(T a) where T : Operand => SetArgument(currArgLen++, a);

        public T[] SetArgument<T>(params T[] args) where T : Operand
        {
            for (int i = 0; i < args.Length; i++) SetArgument(currArgLen++, args[i]);
            return args;
        }

        public T SetArgument<T>(int index, T a) where T : Operand
        {
            if (index < 0 || index >= Arguments.Length) throw new ArgumentException();
            Arguments[index] = a;
            return a;
        }

        public T SetReturn<T>(T r) where T : Operand => SetReturn(currRetLen++, r);

        public T SetReturn<T>(int i, T r) where T : Operand
        {
            if (i < 0 || i >= Return.Length) throw new ArgumentException();
            Return[i] = r;
            return r;
        }

        internal Operand Target { get; set; }

        internal RegisterMask UsedArgs
        {
            get
            {
                if (_usedArgs != null)
                {
                    return _usedArgs;
                }
                var regs = new RegisterMask();
                var argCount = FunctionDeclaration.ArgumentCount;

                for (var i = 0; i < argCount; i++)
                {
                    var arg = FunctionDeclaration.GetArgument(i);
                    if (arg.RegisterIndex == RegisterIndex.Invalid) continue;
                    regs.Or(arg.VariableType.GetRegisterClass(), Utils.Mask(arg.RegisterIndex));
                }

                return _usedArgs = regs;
            }
        }

        internal Operand[] Return { get; private set; }

        internal Operand[] Arguments { get; private set; }

        internal FunctionDeclaration FunctionDeclaration { get; private set; }

    }
}