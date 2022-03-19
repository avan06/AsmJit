using AsmJit.Common;
using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext
{
    public partial class CodeContext
    {
        private static InstructionId[] _condToCmovcc = { InstructionId.Cmovo, InstructionId.Cmovno, InstructionId.Cmovb, InstructionId.Cmovae, InstructionId.Cmove, InstructionId.Cmovne, InstructionId.Cmovbe, InstructionId.Cmova, InstructionId.Cmovs, InstructionId.Cmovns, InstructionId.Cmovpe, InstructionId.Cmovpo, InstructionId.Cmovl, InstructionId.Cmovge, InstructionId.Cmovle, InstructionId.Cmovg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        private static InstructionId[] _condToJcc = { InstructionId.Jo, InstructionId.Jno, InstructionId.Jb, InstructionId.Jae, InstructionId.Je, InstructionId.Jne, InstructionId.Jbe, InstructionId.Ja, InstructionId.Js, InstructionId.Jns, InstructionId.Jpe, InstructionId.Jpo, InstructionId.Jl, InstructionId.Jge, InstructionId.Jle, InstructionId.Jg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        private static InstructionId[] _condToSetcc = { InstructionId.Seto, InstructionId.Setno, InstructionId.Setb, InstructionId.Setae, InstructionId.Sete, InstructionId.Setne, InstructionId.Setbe, InstructionId.Seta, InstructionId.Sets, InstructionId.Setns, InstructionId.Setpe, InstructionId.Setpo, InstructionId.Setl, InstructionId.Setge, InstructionId.Setle, InstructionId.Setg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };

        private static InstructionId CondToCmovcc(Condition cond) => _condToCmovcc[(int)cond];

        private static InstructionId CondToJcc(Condition cond) => _condToJcc[(int)cond];

        private static InstructionId CondToSetcc(Condition cond) => _condToSetcc[(int)cond];

        public void Emit(InstructionId instructionId, params Operand[] ops) => Compiler.Emit(instructionId, ops);

        public void Int3() => Compiler.Emit(InstructionId.Int, (Immediate)3);

        public void Jmp(Pointer dst) => Compiler.Emit(InstructionId.Jmp, new Immediate((long)dst));//Jmp(new Immediate((long)dst));

        public void Ret(Operand o0 = null, Operand o1 = null) => Compiler.CreateReturn(o0, o1);
    }
}
