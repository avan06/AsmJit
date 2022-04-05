using System;
using System.Linq;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    public sealed class Compiler : CompilerBase
    {
        private readonly ConstantPool _localConstPool = new ConstantPool();
        private readonly ConstantPool _globalConstPool = new ConstantPool();

        private FunctionNode _function;
        private CodeProcessor _codeProcessor;
        private CodeContext _codeContext;

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
            compiler._codeProcessor = new CodeProcessor(compiler._assemblerBase, compiler, compiler._codeContext);

            compiler.BeginFunction(args.Select(a => a.GetVariableType()).ToArray(), ret.GetVariableType(), convention);
            return ctx;
        }

        internal void CreateReturn(Operand o0 = null, Operand o1 = null) => AddNode(new ReturnNode(o0, o1));

        internal T SetArgument<T>(int i, T v) where T : Variable
        {
            if (!v.Id.IsVariableId()) throw new ArgumentException();

            var vd = GetVariableData(v.Id);
            _function.SetArgument(i, vd);
            return v;
        }

        internal Label GetEntryLabel() => new Label(_function.Entry.LabelId);

        internal Label GetExitLabel() => new Label(_function.Exit.LabelId);

        internal void Embed(Pointer data, int size) => AddNode(CreateDataNode(data, size));

        internal void Spill(Variable variable)
        {
            if (variable.Id == Constants.InvalidId) return;
            var node = AddNode(CreateHintNode(variable, VariableHint.Spill, Constants.InvalidValue));
        }

        internal void Allocate(Variable variable, Register r = null)
        {
            if (variable.Id == Constants.InvalidId) return;
            HintNode node = AddNode(r == null ?
                CreateHintNode(variable, VariableHint.Spill, Constants.InvalidValue) :
                CreateHintNode(variable, VariableHint.Alloc, r.Index));
        }

        internal void Unuse(Variable variable)
        {
            if (variable.Id == Constants.InvalidId) return;
            var node = AddNode(CreateHintNode(variable, VariableHint.Unuse, Constants.InvalidValue));
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
            var node = AddNode(fn == null ?
                new CallNode(op, _function.FunctionDeclaration) :
                new CallNode(op, fn));
            return node;
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
            return _assemblerBase.Make();
        }

        internal Pointer EndFunction(out int codeSize)
        {
            EndFunctionNode();
            return _assemblerBase.Make(out codeSize);
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

        internal void Align(AligningMode mode, int offset) => AddNode(new AlignNode(mode, offset));

        private HintNode CreateHintNode(Variable var, VariableHint hint, int value)
        {
            var varData = GetVariableData(var.Id);
            var node = new HintNode(varData, hint, value);
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
    }
}