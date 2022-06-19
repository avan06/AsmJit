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

                int size = 0;
                List<Operand> ops = new List<Operand>();
                for (int idx2 = 1; idx2 < insts.Length; idx2++)
                {
                    int shift = 0;
                    int segment = 0;
                    dynamic operand;
                    var opName = insts[idx2];
                    
                    if (Regex.IsMatch(opName, "byte|word|dword|qword|tword", RegexOptions.IgnoreCase))
                    {
                        if (opName.ToLower() == "byte") size = 1;
                        else if (opName.ToLower() == "word") size = 2;
                        else if (opName.ToLower() == "dword") size = 4;
                        else if (opName.ToLower() == "qword") size = 8;
                        else if (opName.ToLower() == "tword") size = 10;
                        continue;
                    }
                    else if (size == 0) size = Constants.X64 ? 8 : 4;

                    if (Regex.Match(opName, @"\[ *([^ \]]+) *([\+\-]) *([^ \]]+) *([\+\-]) *([^ \]]+) *\]") is Match m1 && m1.Success)
                    { //op0 +/- op1 +/- op2
                        (dynamic op0, int sh0, int seg0) = OpParse(m1.Groups[1].Value);
                        (dynamic op1, int sh1, int seg1) = OpParse(m1.Groups[3].Value, m1.Groups[2].Value == "-", OpType.Int);
                        (dynamic op2, int sh2, int seg2) = OpParse(m1.Groups[5].Value, m1.Groups[4].Value == "-", OpType.Int);

                        if (sh0 > 0) shift = sh0;
                        else if (sh1 > 0) shift = sh1;
                        else if (sh2 > 0) shift = sh2;
                        if (seg0 > 0) segment = seg0;
                        else if (seg1 > 0) segment = seg1;
                        else if (seg2 > 0) segment = seg2;

                        operand = Memory.Ptr(op0, op1, 0, op2, size);
                    }
                    else if (Regex.Match(opName, @"\[ *([^ \]]+) *([\+\-]) *([^ \]]+) *\]") is Match m2 && m2.Success)
                    { //op0 +/- op1
                        (dynamic op0, int sh0, int seg0) = OpParse(m2.Groups[1].Value);
                        (dynamic op1, int sh1, int seg1) = OpParse(m2.Groups[3].Value, m2.Groups[2].Value == "-", OpType.Int);

                        if (sh0 > 0) shift = sh0;
                        else if (sh1 > 0) shift = sh1;
                        if (seg0 > 0) segment = seg0;
                        else if (seg1 > 0) segment = seg1;

                        operand = Memory.Ptr(op0, op1, size);
                    }
                    else if (Regex.Match(opName, @"\[ *([\+\-]*) *([^ \]]+) *\]") is Match m3 && m3.Success)
                    { //+/- op0
                        (dynamic op0, int sh0, int seg0) = OpParse(m3.Groups[2].Value, m3.Groups[1].Value == "-", OpType.Ptr);

                        if (sh0 > 0) shift = sh0;
                        if (seg0 > 0) segment = seg0;

                        if (op0 is Register) operand = Memory.Ptr(op0, 0, size);
                        else operand = Constants.X64 ? Memory.QWordAbs(op0) : Memory.DWordAbs(op0);
                    }
                    else (operand, _, _) = OpParse(opName);

                    if (operand != null)
                    {
                        if (shift > 0) ((Memory)operand).Shift = shift;
                        if (segment > 0) ((Memory)operand).Segment = segment;
                        ops.Add(operand);
                    }
                    size = 0;
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

        private (dynamic op, int shift, int segment) OpParse(string opStr, bool negative = false, OpType opType = OpType.None)
        {
            int shift = 0;
            int segment = 0;
            dynamic result = null;
            opStr = (opStr ?? "").Trim();
            if (opStr.StartsWith("0x")) result = long.Parse(opStr.Substring(2), NumberStyles.HexNumber);
            else if (Regex.IsMatch(opStr, @"^\d+$")) result = long.Parse(opStr);

            if (result != null)
            {
                if (opType == OpType.Int) result = (int)result * (negative ? -1 : 1);
                else if (opType == OpType.Long) result = (long)result * (negative ? -1 : 1);
                else if (opType == OpType.Ptr) result = (IntPtr)(int)(result * (negative ? -1 : 1));
                else result = (Immediate)(result * (negative ? -1 : 1));
            }
            else
            { //Parse Registers
                if (Regex.Match(opStr, @"^([^\*]+)\*(\d+)$") is Match m1 && m1.Success)
                { //REG*2、REG*4、REG*8
                    opStr = m1.Groups[1].Value;
                    if (m1.Groups[2].Value == "2") shift = 1;
                    else if (m1.Groups[2].Value == "4") shift = 2;
                    else if (m1.Groups[2].Value == "8") shift = 3;
                }
                else if (Regex.Match(opStr, @"^([^:]+):(\w+)$") is Match m2 && m2.Success)
                { //Segment:REG
                    opStr = m2.Groups[2].Value;
                    if (m2.Groups[1].Value == "es") segment = 1;
                    else if (m2.Groups[1].Value == "cs") segment = 2;
                    else if (m2.Groups[1].Value == "ss") segment = 3;
                    else if (m2.Groups[1].Value == "ds") segment = 4;
                    else if (m2.Groups[1].Value == "fs") segment = 5;
                    else if (m2.Groups[1].Value == "gs") segment = 6;
                }
                FieldInfo opInfo = typeof(Cpu.RegistersClass).GetField(opStr, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                result = opInfo != null ? (Operand)opInfo.GetValue(Cpu.Registers) : null;
            }
            return (result, shift, segment);
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