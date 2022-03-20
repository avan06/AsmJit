using AsmJit.Common.Enums;

namespace AsmJit.Common
{
    public class CodeContextBase
    {
        public InstructionId[] _condToCmovcc = { InstructionId.Cmovo, InstructionId.Cmovno, InstructionId.Cmovb, InstructionId.Cmovae, InstructionId.Cmove, InstructionId.Cmovne, InstructionId.Cmovbe, InstructionId.Cmova, InstructionId.Cmovs, InstructionId.Cmovns, InstructionId.Cmovpe, InstructionId.Cmovpo, InstructionId.Cmovl, InstructionId.Cmovge, InstructionId.Cmovle, InstructionId.Cmovg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        public InstructionId[] _condToJcc = { InstructionId.Jo, InstructionId.Jno, InstructionId.Jb, InstructionId.Jae, InstructionId.Je, InstructionId.Jne, InstructionId.Jbe, InstructionId.Ja, InstructionId.Js, InstructionId.Jns, InstructionId.Jpe, InstructionId.Jpo, InstructionId.Jl, InstructionId.Jge, InstructionId.Jle, InstructionId.Jg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        public InstructionId[] _condToSetcc = { InstructionId.Seto, InstructionId.Setno, InstructionId.Setb, InstructionId.Setae, InstructionId.Sete, InstructionId.Setne, InstructionId.Setbe, InstructionId.Seta, InstructionId.Sets, InstructionId.Setns, InstructionId.Setpe, InstructionId.Setpo, InstructionId.Setl, InstructionId.Setge, InstructionId.Setle, InstructionId.Setg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };

        public InstructionId CondToCmovcc(Condition cond) => _condToCmovcc[(int)cond];

        public InstructionId CondToJcc(Condition cond) => _condToJcc[(int)cond];

        public InstructionId CondToSetcc(Condition cond) => _condToSetcc[(int)cond];
    }
}
