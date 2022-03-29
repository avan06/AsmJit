using System;
using System.Collections.Generic;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    public sealed class CodeContext<T> : CodeContext
    {
        private Type _delegateType;

        internal CodeContext(Compiler compiler, Type delegateType) : base(compiler) { _delegateType = delegateType; }

        public T Compile()
        {
            var fp = Compiler.EndFunction();
            return fp.ToCallable<T>(_delegateType);
        }

        public T Compile(out IntPtr raw, out int codeSize)
        {
            var fp = Compiler.EndFunction(out codeSize);
            raw = fp;
            return fp.ToCallable<T>(_delegateType);
        }
    }

    public partial class CodeContext : CodeContextBase
    {
        private int currArgLen;

        protected Compiler Compiler;

        internal CodeContext(Compiler compiler) => Compiler = compiler;

        public Label Entry { get => Compiler.GetEntryLabel(); }

        public Label Exit { get => Compiler.GetExitLabel(); }

        public T SetArgument<T>(T v) where T : Variable => SetArgument(currArgLen++, v);

        public T[] SetArgument<T>(params T[] args) where T : Variable
        {
            for (int i = 0; i < args.Length; i++) SetArgument(currArgLen++, args[i]);
            return args;
        }

        public T SetArgument<T>(int index, T v) where T : Variable => Compiler.SetArgument(index, v);

        public Label Label() => Compiler.CreateLabel();

        public CallNode Call(GpVariable target, FnPointer fn) => Compiler.CreateCall(target, fn.FunctionDeclaration);

        public CallNode Call(Label target) => Compiler.CreateCall(target);

        public CallNode Call(FnPointer fn)
        {
            var target = IntPtr("fn");
            Compiler.Emit(Inst.Mov, target, fn);
            return Compiler.CreateCall(target, fn.FunctionDeclaration);
        }

        public CodeContext Unfollow()
        {
            Compiler.Unfollow();
            return this;
        }

        public void Bind(Label label) => Compiler.Bind(label);

        public void Spill(Variable variable) => Compiler.Spill(variable);

        public void Unuse(Variable variable) => Compiler.Unuse(variable);

        public void Allocate(Variable variable) => Compiler.Allocate(variable);

        public void Allocate(Variable variable, Register r) => Compiler.Allocate(variable, r);

        public void Align(AligningMode mode, int size) => Compiler.Align(mode, size);

        public void Data(Label label, int alignment, params Data[] data) => Compiler.Data(label, alignment, data);

        //		public void Data(params DataBlock[] data) => Compiler.Data(data);
        //
        //		public void DataBlock(Action<DataContext> dataBlock) => Compiler.Data(dataBlock);

        public StackMemory Stack(int size, int alignment, string name = null) => Compiler.CreateStack(size, alignment, name);

        public GpVariable Int8(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.Int8, name);

        public GpVariable UInt8(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.UInt8, name);

        public GpVariable Int16(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.Int16, name);

        public GpVariable UInt16(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.UInt16, name);

        public GpVariable Int32(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.Int32, name);

        public GpVariable UInt32(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.UInt32, name);

        public GpVariable Int64(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.Int64, name);

        public GpVariable UInt64(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.UInt64, name);

        public GpVariable IntPtr(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.IntPtr, name);

        public GpVariable UIntPtr(string name = null) => Compiler.CreateVariable<GpVariable>(VariableType.UIntPtr, name);

        public XmmVariable Xmm(string name = null) => Compiler.CreateVariable<XmmVariable>(VariableType.Xmm, name);

        public XmmVariable XmmSs(string name = null) => Compiler.CreateVariable<XmmVariable>(VariableType.XmmSs, name);

        public XmmVariable XmmSd(string name = null) => Compiler.CreateVariable<XmmVariable>(VariableType.XmmSd, name);

        public Memory Int32Const(ConstantScope scope, int v)
        {
            unsafe
            {
                var p = &v;
                return Compiler.CreateConstant(scope, p, 4);
            }
        }

        public void Emit(InstInfo instructionId, params dynamic[] insts)
        {
            List<Operand> ops = new List<Operand>();
            for (int i = 0; i < insts.Length; i++)
            {
                if (insts[i].GetType() == typeof(InstInfo))
                {
                    Compiler.Emit(instructionId, ops.ToArray());
                    instructionId = insts[i];
                    ops.Clear();
                    continue;
                }
                ops.Add(insts[i]);
            }
            Compiler.Emit(instructionId, ops.ToArray());
        }

        public void Int3() => Compiler.Emit(Inst.Int, (Immediate)3);

        public void Jmp(Pointer dst) => Compiler.Emit(Inst.Jmp, new Immediate((long)dst));

        public void Ret(Operand o0 = null, Operand o1 = null) => Compiler.CreateReturn(o0, o1);
    }
}
