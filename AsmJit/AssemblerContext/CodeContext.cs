using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;

namespace AsmJit.AssemblerContext
{
    public sealed class CodeContext<T> : CodeContext
    {
        private Type _delegateType;

        internal CodeContext(Assembler assembler, Type delegateType) : base(assembler) => _delegateType = delegateType;

        public T Compile()
        {
            var fp = Assembler.Make();
            return fp.ToCallable<T>(_delegateType);
        }

        public T Compile(out IntPtr raw, out int codeSize)
        {
            var fp = Assembler.Make(out codeSize);
            raw = fp;
            return fp.ToCallable<T>(_delegateType);
        }
    }

    public class CodeContext : CodeContextBase
    {
        protected Assembler Assembler;

        internal CodeContext(Assembler assembler) => Assembler = assembler;

        public Label Label() => Assembler.CreateLabel();

        public void Bind(Label label) => Assembler.Bind(label.Id);

        public void Align(AligningMode mode, int size) => Assembler.Align(mode, size);

        public void Data(Label label, params Data[] data) => Data(label, 16, data);

        public void Data(Label label, int alignment, params Data[] data) => Assembler.Data(label, alignment, data);

        public void Emit(string instIdStr, params Operand[] ops)
        {
            FieldInfo fieldInfo = typeof(Inst).GetField(instIdStr, BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
            if (fieldInfo == null) fieldInfo = typeof(Inst).GetField(instIdStr, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
            if (fieldInfo == null) throw new ArgumentException(string.Format("InstructionId({0}) is invalid", instIdStr), "InstructionId");
            InstInfo instructionId = (InstInfo)fieldInfo.GetValue(null);
            Emit(instructionId, ops);
        }

        public void Emit(string instStr)
        {
            var instArr = instStr.Replace("\r\n", "\n").Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var instRaw in instArr)
            {
                var insts = instRaw.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (insts[0].ToLower() == "int3")
                {
                    Int3();
                    continue;
                }

                FieldInfo fieldInfo = typeof(Inst).GetField(insts[0], BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                if (fieldInfo == null) fieldInfo = typeof(Inst).GetField(insts[0], BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                if (fieldInfo == null) throw new ArgumentException(string.Format("InstructionId({0}) is invalid", insts[0]), "InstructionId");
                InstInfo instructionId = (InstInfo)fieldInfo.GetValue(null);

                List<Operand> ops = new List<Operand>();
                for (int idx2 = 1; idx2 < insts.Length; idx2++)
                {
                    dynamic operand;
                    var opName = insts[idx2];
                    if (Regex.Match(opName, @"\[ *([^ \]]+) *([\+\-]) *([^ \]]+) *([\+\-]) *([^ \]]+) *\]") is Match m1 && m1.Success)
                    {
                        var op0 = OpParse(m1.Groups[1].Value);
                        var op1 = OpParse(m1.Groups[3].Value, m1.Groups[2].Value == "-", OpType.Int);
                        var op2 = OpParse(m1.Groups[5].Value, m1.Groups[4].Value == "-", OpType.Int);
                        operand = Constants.X64 ? Memory.QWord(op0, op1, 0, op2) : Memory.DWord(op0, op1, 0, op2);
                    }
                    else if (Regex.Match(opName, @"\[ *([^ \]]+) *([\+\-]) *([^ \]]+) *\]") is Match m2 && m2.Success)
                    {
                        var op0 = OpParse(m2.Groups[1].Value);
                        var op1 = OpParse(m2.Groups[3].Value, m2.Groups[2].Value == "-", OpType.Int);
                        operand = Constants.X64 ? Memory.QWord(op0, op1) : Memory.DWord(op0, op1);
                    }
                    else if (Regex.Match(opName, @"\[ *([\+\-]*) *([^ \]]+) *\]") is Match m3 && m3.Success)
                    {
                        var op0 = OpParse(m3.Groups[2].Value, m3.Groups[1].Value == "-", OpType.Ptr);
                        operand = Constants.X64 ? Memory.QWordAbs(op0) : Memory.DWordAbs(op0);
                    }
                    else operand = OpParse(opName);
                    if (operand != null) ops.Add(operand);
                }
                Emit(instructionId, ops.ToArray());
            }
        }
        private enum OpType
        {
            None,
            Int,
            Long,
            Ptr,
        }

        private dynamic OpParse(string opStr, bool negative = false, OpType opType = OpType.None)
        {
            dynamic result = null;
            opStr = (opStr ?? "").Trim();
            if (opStr.StartsWith("0x")) result = long.Parse(opStr.Substring(2), NumberStyles.HexNumber);
            else if (Regex.IsMatch(opStr, @"^\d+$")) result = long.Parse(opStr);

            if (result != null)
            {
                if (opType == OpType.Int) result = (int)result;
                else if (opType == OpType.Long) result = (long)result;
                else if (opType == OpType.Ptr) result = (IntPtr)(int)result;
                else result = (Immediate)(result * (negative ? -1 : 1));
            }
            else
            { //Parse Registers
                FieldInfo opInfo = typeof(Cpu.RegistersClass).GetField(opStr, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                result = opInfo != null ? (Operand)opInfo.GetValue(Cpu.Registers) : null;
            }
            return result;
        }

        public void Emit(InstInfo instructionId, params Operand[] ops) => Assembler.Emit(instructionId, ops);

        public void Call(Operand o0) => Assembler.Emit(Inst.Call, o0);

        public void Call(IntPtr o0) => Assembler.Emit(Inst.Call, (Immediate)o0.ToInt64());//Call(o0.ToInt64());

        public void Int3() => Assembler.Emit(Inst.Int, (Immediate)3);

        public void Jmp(IntPtr dst) => Assembler.Emit(Inst.Jmp, (Immediate)dst.ToInt64());//Jmp((Immediate)dst.ToInt64());

        public void Ret() => Assembler.Emit(Inst.Ret);

        public void Ret(Immediate o0) => Assembler.Emit(Inst.Ret, o0);

        public void Ret(long o0) => Assembler.Emit(Inst.Ret, (Immediate)o0);

        public void Ret(ulong o0) => Assembler.Emit(Inst.Ret, (Immediate)o0);
    }
}