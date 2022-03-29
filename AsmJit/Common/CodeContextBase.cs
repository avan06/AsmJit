using AsmJit.Common.Enums;

namespace AsmJit.Common
{
    public class CodeContextBase
    {
        public InstInfo[] _condToCmovcc = { Inst.Cmovo, Inst.Cmovno, Inst.Cmovb, Inst.Cmovae, Inst.Cmove, Inst.Cmovne, Inst.Cmovbe, Inst.Cmova, Inst.Cmovs, Inst.Cmovns, Inst.Cmovpe, Inst.Cmovpo, Inst.Cmovl, Inst.Cmovge, Inst.Cmovle, Inst.Cmovg, Inst.None, Inst.None, Inst.None, Inst.None };
        public InstInfo[] _condToJcc = { Inst.Jo, Inst.Jno, Inst.Jb, Inst.Jae, Inst.Je, Inst.Jne, Inst.Jbe, Inst.Ja, Inst.Js, Inst.Jns, Inst.Jpe, Inst.Jpo, Inst.Jl, Inst.Jge, Inst.Jle, Inst.Jg, Inst.None, Inst.None, Inst.None, Inst.None };
        public InstInfo[] _condToSetcc = { Inst.Seto, Inst.Setno, Inst.Setb, Inst.Setae, Inst.Sete, Inst.Setne, Inst.Setbe, Inst.Seta, Inst.Sets, Inst.Setns, Inst.Setpe, Inst.Setpo, Inst.Setl, Inst.Setge, Inst.Setle, Inst.Setg, Inst.None, Inst.None, Inst.None, Inst.None };

        public InstInfo CondToCmovcc(Condition cond) => _condToCmovcc[(int)cond];

        public InstInfo CondToJcc(Condition cond) => _condToJcc[(int)cond];

        public InstInfo CondToSetcc(Condition cond) => _condToSetcc[(int)cond];
    }
}
