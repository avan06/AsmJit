using System;
using System.Collections.Generic;
using System.Linq;
using AsmJit.AssemblerContext;
using AsmJit.Common;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext.CodeTree;
using CallingConvention = AsmJit.Common.CallingConvention;

namespace AsmJit.CompilerContext
{
    public sealed class Compiler : IDisposable
    {
        private List<VariableData> _variables = new List<VariableData>();
        private ConstantPool _localConstPool = new ConstantPool();
        private ConstantPool _globalConstPool = new ConstantPool();
        private Label _localConstPoolLabel;
        private Label _globalConstPoolLabel;		
        private CodeNode _firstNode;
        private CodeNode _lastNode;
        private CodeNode _currentNode;
        private FunctionNode _function;
        private CodeProcessor _codeProcessor;
        private Assembler _assembler;
        private CodeContext _codeContext;
        private List<Pointer> _dataAllocations = new List<Pointer>();
        private List<DataBlock> _data = new List<DataBlock>();

        private Compiler() => _assembler = new Assembler();

        public static CodeContext<T> CreateContext<T>(CallingConvention convention = CallingConvention.Default)
        {
            var t = typeof(T);

            var args = new Type[0];
            Type ret = null;
            Type delType;
            if (t == typeof(Action)) delType = DelegateCreator.NewDelegateType(args);
            else if (Utils.Actions.Contains(t.GetGenericTypeDefinition()))
            {
                var gargs = t.GetGenericArguments();
                args = new Type[gargs.Length].InitializeWith(i => gargs[i]);
                delType = DelegateCreator.NewDelegateType(args);
            }
            else if (Utils.Funcs.Contains(t.GetGenericTypeDefinition()))
            {
                var gargs = t.GetGenericArguments();
                args = new Type[gargs.Length - 1].InitializeWith(i => gargs[i]);
                ret = gargs.Last();
                delType = DelegateCreator.NewDelegateType(ret, args);
            }
            else throw new ArgumentException(String.Format("unknown type:{0}", t));

            var compiler = new Compiler();
            var ctx = new CodeContext<T>(compiler, delType);
            compiler._codeContext = ctx;
            compiler._codeProcessor = new CodeProcessor(compiler._assembler, compiler, compiler._codeContext);

            compiler.BeginFunction(args.Select(a => a.GetVariableType()).ToArray(), ret.GetVariableType(), convention);
            return ctx;
        }

        internal void CreateReturn(Operand o0 = null, Operand o1 = null)
        {
            var node = new ReturnNode(o0, o1);
            AddNode(node);
        }
        
        internal T SetArgument<T>(int i, T v) where T : Variable
        {
            if (!v.Id.IsVariableId()) throw new ArgumentException();

            var vd = GetVariableData(v.Id);
            _function.SetArgument(i, vd);
            return v;
        }

        internal Label GetEntryLabel() => new Label(_function.Entry.LabelId);

        internal Label GetExitLabel() => new Label(_function.Exit.LabelId);

        internal void Unfollow() => _assembler.Unfollow();

        public void Data(Label label, int alignment, params Data[] data) => _data.Add(new DataBlock(label, alignment, data));

        internal void Embed(Pointer data, int size)
        {
            var node = CreateDataNode(data, size);
            AddNode(node);
        }

        internal void Bind(Label label)
        {
            var node = _assembler.GetLabelData(label.Id).ContextData;
            if (node == null) throw new ArgumentException();
            AddNode(node);
        }

        internal void Spill(Variable variable)
        {
            if (variable.Id == Constants.InvalidId) return;
            var node = CreateHintNode(variable, VariableHint.Spill, Constants.InvalidValue);
            AddNode(node);
        }

        internal void Allocate(Variable variable, Register r = null)
        {
            if (variable.Id == Constants.InvalidId) return;
            HintNode node = r == null ?
                CreateHintNode(variable, VariableHint.Spill, Constants.InvalidValue) : 
                CreateHintNode(variable, VariableHint.Alloc, r.Index);
            AddNode(node);
        }

        internal void Unuse(Variable variable)
        {
            if (variable.Id == Constants.InvalidId) return;
            var node = CreateHintNode(variable, VariableHint.Unuse, Constants.InvalidValue);
            AddNode(node);
        }

        internal StackMemory CreateStack(int size, int alignment, string name = null)
        {
            if (size == 0) throw new ArgumentException("Invalid alignment");
            alignment = Math.Min(alignment, 64);
            var vd = CreateVariableData(VariableType.Stack, new VariableInfo(RegisterType.Invalid, size, 0, 0, name), name, alignment);
            return new StackMemory(MemoryType.StackIndex, vd.Id);
        }

        internal Memory CreateConstant(ConstantScope scope, Pointer data, int size)
        {
            var offset = 0;
            Label dstLabel;
            ConstantPool dstPool;
            switch (scope)
            {
                case ConstantScope.Local:
                    if (_localConstPoolLabel == null || _localConstPoolLabel.Id == Constants.InvalidId) _localConstPoolLabel = CreateLabel();
                    dstLabel = _localConstPoolLabel;
                    dstPool = _localConstPool;
                    break;
                case ConstantScope.Global:
                    if (_globalConstPoolLabel == null || _globalConstPoolLabel.Id == Constants.InvalidId) _globalConstPoolLabel = CreateLabel();
                    dstLabel = _globalConstPoolLabel;
                    dstPool = _globalConstPool;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            dstPool.Add(data, size, ref offset);
            return Memory.Ptr(dstLabel, offset, size);
        }

        internal CallNode CreateCall(Operand op, FunctionDeclaration fn = null)
        {
            var node = fn == null ?
                new CallNode(op, _function.FunctionDeclaration) :
                new CallNode(op, fn);
            AddNode(node);
            return node;
        }

        internal VariableData CreateVariableData(VariableType type, VariableInfo info, string name, int alignment = 0)
        {
            var varData = new VariableData(type, info, _variables.Count.AsVariableId(), name, alignment);
            _variables.Add(varData);
            return varData;
        }

        private void EmbedConstantPool(Label label, ConstantPool pool)
        {
            if (label.Id == Constants.InvalidId) throw new ArgumentException("Invalid variable id");

            Align(AligningMode.Data, pool.Alignment);
            Bind(label);

            var embedNode = CreateDataNode(Pointer.Invalid, pool.Size);

            pool.Fill(embedNode.Data);
            AddNode(embedNode);
        }

        private void BeginFunction(VariableType[] args, VariableType ret, CallingConvention callingConvention)
        {
            _function = new FunctionNode(CreateLabelNode(), CreateLabelNode(), args, ret, callingConvention);

            AddNode(_function);
            AddNode(_function.Entry);
            var node = _currentNode;
            AddNode(_function.Exit); // Add function exit / epilog marker.
            AddNode(_function.End); // Add function end.
            SetCurrentNode(node);
        }

        internal Pointer EndFunction()
        {
            EndFunctionNode();
            return _assembler.Make();
        }

        internal Pointer EndFunction(out int codeSize)
        {
            EndFunctionNode();
            return _assembler.Make(out codeSize);
        }

        internal void EndFunctionNode()
        {
            // Add local constant pool at the end of the function (if exist).
            SetCurrentNode(_function.Exit);

            _function.FunctionFlags |= FunctionNodeFlags.IsFinished;
            var func = _function;
            _function = null;

            SetCurrentNode(func.End);
            unsafe
            {
                foreach (DataBlock dataItem in _data)
                {
                    if (dataItem.Label == null) throw new ArgumentException("DataBlock label is null");
                    Align(AligningMode.Data, dataItem.Alignment);
                    Bind(dataItem.Label);
                    foreach (Data v in dataItem.Data)
                    {
                        fixed (byte* pv = v.ByteData)
                        {
                            Embed(pv, v.ByteData.Length);
                        }
                    }
                }
            }
            Finish();
        }

        internal CodeNode SetCurrentNode(CodeNode node)
        {
            CodeNode old = _currentNode;
            _currentNode = node;
            return old;
        }

        internal CodeNode GetCurrentNode() => _currentNode;

        internal VariableData GetVariableData(int id)
        {
            id = id.GetActualVariableId();
            if (id == Constants.InvalidId || id >= _variables.Count) throw new ArgumentException("Invalid id");
            return _variables[id];
        }

        internal void Align(AligningMode mode, int offset)
        {
            var node = new AlignNode(mode, offset);
            AddNode(node);
        }

        internal void AddNode(CodeNode node)
        {
            if (node == null) throw new ArgumentException("CodeNode is null");
            if (node.Previous != null || node.Next != null) throw new ArgumentException("Node already inserted");

            if (_currentNode == null)
            {
                if (_firstNode == null)
                {
                    _firstNode = node;
                    _lastNode = node;
                }
                else
                {
                    node.Next = _firstNode;
                    _firstNode.Previous = node;
                    _firstNode = node;
                }
            }
            else
            {
                var prev = _currentNode;
                var next = _currentNode.Next;

                node.Previous = prev;
                node.Next = next;

                prev.Next = node;
                if (next != null) next.Previous = node;
                else _lastNode = node;
            }
            _currentNode = node;
        }

        internal void AddNodeBefore(CodeNode node, CodeNode @ref)
        {
            if (node == null || @ref == null) throw new ArgumentException("CodeNode is null");
            if (node.Previous != null || node.Next != null) throw new ArgumentException("Node already inserted");

            var prev = @ref.Previous;
            var next = @ref;

            node.Previous = prev;
            node.Next = next;

            next.Previous = node;
            if (prev != null) prev.Next = node;
            else _firstNode = node;
        }

        internal void RemoveNode(CodeNode node)
        {
            var prev = node.Previous;
            var next = node.Next;

            if (_firstNode == node) _firstNode = next;
            else prev.Next = next;

            if (_lastNode == node) _lastNode = prev;
            else next.Previous = prev;

            node.Previous = null;
            node.Next = null;

            if (_currentNode == node) _currentNode = prev;
            AfterRemoveNode(node);
        }

        private static void AfterRemoveNode(CodeNode node)
        {
            if (!node.Flags.IsSet(CodeNodeFlags.Jmp | CodeNodeFlags.Jcc)) return;

            var jj = new ValueSet<CodeNode, JumpNode>();

            var jump = node.As<JumpNode>();
            var label = jump.Target;

            if (label == null) return;
            // Disconnect.
            jj.Value0 = label;
            jj.Value1 = label.From;
            for (; ; )
            {
                var current = jj;
                if (current.Value1 == null) break;

                if (current.Value1 == jump)
                {
                    jj.Value0 = jump;
                    jj.Value1 = jump.NextJump;
                    break;
                }

                jj.Value1 = current.Value1;
            }
            if (jj.Value0.Type == CodeNodeType.Label) jj.Value0.As<LabelNode>().From = jj.Value1;
            else jj.Value0.As<JumpNode>().NextJump = jj.Value1;
            label.ReferenceCount--;
        }

        internal Label CreateLabel() => new Label(CreateLabelNode().LabelId);

        private HintNode CreateHintNode(Variable var, VariableHint hint, int value)
        {
            var varData = GetVariableData(var.Id);
            var node = new HintNode(varData, hint, value);
            return node;
        }

        internal LabelNode CreateLabelNode()
        {
            int labelId;
            var data = _assembler.CreateLabelData(out labelId);
            var node = new LabelNode(labelId);
            data.ContextData = node;
            return node;
        }		

        private DataNode CreateDataNode(Pointer data, int size)
        {
            var clonedData = UnsafeMemory.Allocate(size);
            if (clonedData == Pointer.Invalid) return null;
            _dataAllocations.Add(clonedData);
            if (data != Pointer.Invalid) UnsafeMemory.Copy(clonedData, data, size);

            return new DataNode(clonedData, size);
        }

        private void Finish()
        {
            if (_localConstPoolLabel != null && _localConstPoolLabel.Id != Constants.InvalidId)
            {
                EmbedConstantPool(_localConstPoolLabel, _localConstPool);
                _localConstPoolLabel.Reset();
                _localConstPool.Reset();
            }
            var node = _firstNode;
            do
            {
                var start = node;

                if (node.Type == CodeNodeType.Function)
                {
                    var func = start.As<FunctionNode>();
                    node = func.End;
                    _codeProcessor.FetchAndTranslate(func);
                }

                do
                {
                    node = node.Next;
                } while (node != null && node.Type != CodeNodeType.Function);

                _codeProcessor.Serialize(start, node);
                //Cleanup();
            } while (node != null);			
        }

        private void CreateInstructionNode(InstructionId instructionId, Operand[] operands)
        {
            var options = _assembler.GetInstructionOptionsAndReset();
            if (instructionId.IsJump())
            {
                LabelNode target = null;
                JumpNode next = null;
                CodeNodeFlags flags = 0;
                if (!options.IsSet(InstructionOptions.Unfollow))
                {
                    if (operands[0].IsLabel()) target = _assembler.GetLabelData(operands[0].Id).ContextData;
                    else options |= InstructionOptions.Unfollow;
                }
                flags |= instructionId == InstructionId.Jmp ? CodeNodeFlags.Jmp | CodeNodeFlags.Taken : CodeNodeFlags.Jcc;

                if (target != null) next = target.From;

                // The 'jmp' is always taken, conditional jump can contain hint, we detect it.
                if (instructionId == InstructionId.Jmp) flags |= CodeNodeFlags.Taken;
                else if (options.IsSet(InstructionOptions.Taken)) flags |= CodeNodeFlags.Taken;

                var node = new JumpNode(instructionId, options, operands);
                node.Flags |= flags;
                node.Target = target;
                if (target != null)
                {
                    node.NextJump = next;
                    target.From = node;
                    target.ReferenceCount++;
                }
                AddNode(node);
                return;
            }
            var inst = new InstructionNode(instructionId, options, operands);
            AddNode(inst);
        }

        internal TV CreateVariable<TV>(VariableType type, string name = null) where TV : Variable
        {
            type = type.GetMappedType();
            if (type.IsInvalid()) throw new ArgumentException();
            var varInfo = type.GetVariableInfo();
            VariableData varData;
            Variable var;
            switch (type)
            {
                case VariableType.Int8:
                case VariableType.UInt8:
                case VariableType.Int16:
                case VariableType.UInt16:
                case VariableType.Int32:
                case VariableType.UInt32:
                case VariableType.Int64:
                case VariableType.UInt64:
                case VariableType.IntPtr:
                case VariableType.UIntPtr:
                    if (typeof(TV) != typeof(GpVariable)) throw new ArgumentException();
                    varData = CreateVariableData(type, varInfo, name);
                    var = new GpVariable((GpVariableType) type, varData.Id);
                    break;
                case VariableType.Fp32:
                case VariableType.Fp64:
                    if (typeof(TV) != typeof(FpVariable)) throw new ArgumentException();
                    varData = CreateVariableData(type, varInfo, name);
                    var = new FpVariable((FpVariableType) type, varData.Id);
                    break;
                case VariableType.Xmm:
                case VariableType.XmmSs:
                case VariableType.XmmSd:
                    if (typeof(TV) != typeof(XmmVariable)) throw new ArgumentException();
                    varData = CreateVariableData(type, varInfo, name);
                    var = new XmmVariable((XmmVariableType) type, varData.Id);
                    break;
                default:
                    throw new ArgumentException();
            }
            return (TV)var;
        }

        internal void Emit(InstructionId instructionId) => CreateInstructionNode(instructionId, new Operand[0]);

        internal void Emit(InstructionId instructionId, params Operand[] ops) => CreateInstructionNode(instructionId, ops);

        internal static GpRegister GpbLo(int index) => new GpRegister(GpRegisterType.GpbLo, index);

        internal static GpRegister GpbHi(int index) => new GpRegister(GpRegisterType.GpbHi, index);

        internal static GpRegister Gpw(int index) => new GpRegister(GpRegisterType.Gpw, index);

        internal static GpRegister Gpd(int index) => new GpRegister(GpRegisterType.Gpd, index);

        internal static GpRegister Gpq(int index) => new GpRegister(GpRegisterType.Gpq, index);

        internal static FpRegister Fp(int index) => new FpRegister(index);

        internal static MmRegister Mm(int index) => new MmRegister(index);

        internal static KRegister K(int index) => new KRegister(index);

        internal static XmmRegister Xmm(int index) => new XmmRegister(index);

        internal static YmmRegister Ymm(int index) => new YmmRegister(index);

        internal static ZmmRegister Zmm(int index) => new ZmmRegister(index);

        public void Dispose()
        {
            foreach (Pointer pointer in _dataAllocations) UnsafeMemory.Free(pointer);
        }
    }
}