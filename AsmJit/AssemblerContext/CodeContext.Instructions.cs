using System;
using AsmJit.Common;
using AsmJit.Common.Operands;

namespace AsmJit.AssemblerContext
{
    public partial class CodeContext
    {
        private static InstructionId[] _condToCmovcc = { InstructionId.Cmovo, InstructionId.Cmovno, InstructionId.Cmovb, InstructionId.Cmovae, InstructionId.Cmove, InstructionId.Cmovne, InstructionId.Cmovbe, InstructionId.Cmova, InstructionId.Cmovs, InstructionId.Cmovns, InstructionId.Cmovpe, InstructionId.Cmovpo, InstructionId.Cmovl, InstructionId.Cmovge, InstructionId.Cmovle, InstructionId.Cmovg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        private static InstructionId[] _condToJcc = { InstructionId.Jo, InstructionId.Jno, InstructionId.Jb, InstructionId.Jae, InstructionId.Je, InstructionId.Jne, InstructionId.Jbe, InstructionId.Ja, InstructionId.Js, InstructionId.Jns, InstructionId.Jpe, InstructionId.Jpo, InstructionId.Jl, InstructionId.Jge, InstructionId.Jle, InstructionId.Jg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };
        private static InstructionId[] _condToSetcc = { InstructionId.Seto, InstructionId.Setno, InstructionId.Setb, InstructionId.Setae, InstructionId.Sete, InstructionId.Setne, InstructionId.Setbe, InstructionId.Seta, InstructionId.Sets, InstructionId.Setns, InstructionId.Setpe, InstructionId.Setpo, InstructionId.Setl, InstructionId.Setge, InstructionId.Setle, InstructionId.Setg, InstructionId.None, InstructionId.None, InstructionId.None, InstructionId.None };

        private static InstructionId CondToCmovcc(Condition cond) => _condToCmovcc[(int)cond];

        private static InstructionId CondToJcc(Condition cond) => _condToJcc[(int)cond];

        private static InstructionId CondToSetcc(Condition cond) => _condToSetcc[(int)cond];

        public void Emit(InstructionId instructionId, params Operand[] ops) => Assembler.Emit(instructionId, ops);

        public void Call(GpRegister o0) => Assembler.Emit(InstructionId.Call, o0);

        public void Call(Memory o0) => Assembler.Emit(InstructionId.Call, o0);

        public void Call(Label o0) => Assembler.Emit(InstructionId.Call, o0);

        public void Call(Immediate o0) => Assembler.Emit(InstructionId.Call, o0);

        public void Call(IntPtr o0) => Assembler.Emit(InstructionId.Call, (Immediate)o0.ToInt64());//Call(o0.ToInt64());

        public void Int3() => Assembler.Emit(InstructionId.Int, (Immediate)3);

        public void Jmp(IntPtr dst) => Assembler.Emit(InstructionId.Jmp, (Immediate)dst.ToInt64());//Jmp((Immediate)dst.ToInt64());

        public void Ret() => Assembler.Emit(InstructionId.Ret);

        public void Ret(Immediate o0) => Assembler.Emit(InstructionId.Ret, o0);

        public void Ret(long o0) => Assembler.Emit(InstructionId.Ret, (Immediate)o0);

        public void Ret(ulong o0) => Assembler.Emit(InstructionId.Ret, (Immediate)o0);
    }
}
