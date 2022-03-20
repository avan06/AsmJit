using System;
using System.Collections.Generic;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.Common.Variables;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    public class CompilerBase : IDisposable
    {
        private List<VariableData> _variables = new List<VariableData>();
        protected Label _localConstPoolLabel;
        protected Label _globalConstPoolLabel;

        private Dictionary<int, LabelNode> labelNodes = new Dictionary<int, LabelNode>();
        protected List<Pointer> _dataAllocations = new List<Pointer>();
        protected List<DataBlock> _data = new List<DataBlock>();
        protected CodeNode _firstNode;
        protected CodeNode _lastNode;
        protected CodeNode _currentNode;
        protected AssemblerBase _assemblerBase;

        protected CompilerBase() => _assemblerBase = new AssemblerBase();

        internal void Data(Label label, int alignment, params Data[] data) => _data.Add(new DataBlock(label, alignment, data));

        internal void Unfollow() => _assemblerBase.Unfollow();

        internal void Bind(Label label)
        {
            var node = labelNodes[label.Id];
            if (node == null) throw new ArgumentException();
            AddNode(node);
        }

        internal StackMemory CreateStack(int size, int alignment, string name = null)
        {
            if (size == 0) throw new ArgumentException("Invalid alignment");
            alignment = Math.Min(alignment, 64);
            var vd = CreateVariableData(VariableType.Stack, new VariableInfo(RegisterType.Invalid, size, 0, 0, name), name, alignment);
            return new StackMemory(MemoryType.StackIndex, vd.Id);
        }

        internal VariableData CreateVariableData(VariableType type, VariableInfo info, string name, int alignment = 0)
        {
            var varData = new VariableData(type, info, _variables.Count.AsVariableId(), name, alignment);
            _variables.Add(varData);
            return varData;
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

        private void AfterRemoveNode(CodeNode node)
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

        internal LabelNode CreateLabelNode()
        {
            _assemblerBase.CreateLabelData(out int labelId);
            var node = new LabelNode(labelId);
            labelNodes.Add(labelId, node);
            return node;
        }

        private void CreateInstructionNode(InstructionId instructionId, Operand[] operands)
        {
            var options = _assemblerBase.GetInstructionOptionsAndReset();
            if (instructionId.IsJump())
            {
                LabelNode target = null;
                JumpNode next = null;
                CodeNodeFlags flags = 0;
                if (!options.IsSet(InstructionOptions.Unfollow))
                {
                    if (operands[0].IsLabel()) target = labelNodes[operands[0].Id];
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
            VariableData varData = CreateVariableData(type, varInfo, name);
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
                    var = new GpVariable((GpVariableType)type, varData.Id);
                    break;
                case VariableType.Fp32:
                case VariableType.Fp64:
                    if (typeof(TV) != typeof(FpVariable)) throw new ArgumentException();
                    var = new FpVariable((FpVariableType)type, varData.Id);
                    break;
                case VariableType.Xmm:
                case VariableType.XmmSs:
                case VariableType.XmmSd:
                    if (typeof(TV) != typeof(XmmVariable)) throw new ArgumentException();
                    var = new XmmVariable((XmmVariableType)type, varData.Id);
                    break;
                default:
                    throw new ArgumentException();
            }
            return (TV)var;
        }

        internal void Emit(InstructionId instructionId) => CreateInstructionNode(instructionId, new Operand[0]);

        internal void Emit(InstructionId instructionId, params Operand[] ops) => CreateInstructionNode(instructionId, ops);

        public void Dispose()
        {
            foreach (Pointer pointer in _dataAllocations) UnsafeMemory.Free(pointer);
        }
    }
}
