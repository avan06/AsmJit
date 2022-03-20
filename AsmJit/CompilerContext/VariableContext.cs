using System.Collections.Generic;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.Common.Variables;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    internal class VariableContext
    {
        private VariableAttributes[] _tmpList = new VariableAttributes[80].InitializeWith(() => new VariableAttributes());

        public readonly VariableState State;

        public readonly Dictionary<RegisterClass, int> GaRegs;

        public readonly List<VariableData> ContextVd;

        public readonly List<CodeNode> ReturningList;

        public int VariableCount;

        public readonly List<CodeNode> JccList;

        public VariableContext()
        {
            JccList = new List<CodeNode>();
            ReturningList = new List<CodeNode>();
            ContextVd = new List<VariableData>();
            ClobberedRegs = new RegisterMask();
            GaRegs = new Dictionary<RegisterClass, int>
            {
                [RegisterClass.Gp] = (int)(Utils.Bits(Cpu.Info.RegisterCount.Gp) & ~Utils.Mask(RegisterIndex.Sp)),
                [RegisterClass.Mm] = (int)Utils.Bits(Cpu.Info.RegisterCount.Mm),
                [RegisterClass.K] = (int)Utils.Bits(Cpu.Info.RegisterCount.K),
                [RegisterClass.Xyz] = (int)Utils.Bits(Cpu.Info.RegisterCount.Xyz)
            };
            State = new VariableState(0);
        }

        public RegisterCount RegCount { get; private set; }

        public RegisterMask InRegs { get; private set; }

        public RegisterMask OutRegs { get; private set; }

        public RegisterMask ClobberedRegs { get; private set; }

        public VariableAttributes this[int i] => _tmpList[i];

        public void Begin()
        {
            RegCount = new RegisterCount();
            InRegs = new RegisterMask();
            OutRegs = new RegisterMask();
            ClobberedRegs = new RegisterMask();
        }

        public VariableAttributes Add(VariableData vd, VariableFlags flags, int newAllocable)
        {
            var va = _tmpList[VariableCount++];
            va.Setup(vd, flags, 0, newAllocable);
            va.UsageCount += 1;
            vd.Attributes = va;

            RegisterContextVariable(vd);
            RegCount.Add(vd.Info.RegisterClass);
            return va;
        }

        public VariableAttributes Merge(VariableData vd, VariableFlags flags, int newAllocable)
        {
            var va = vd.Attributes;
            if (va == null)
            {
                va = _tmpList[VariableCount++];
                va.Setup(vd, flags, 0, newAllocable);
                vd.Attributes = va;

                RegisterContextVariable(vd);
                RegCount.Add(vd.Info.RegisterClass);
            }
            va.Flags |= flags;
            va.UsageCount++;
            return va;
        }

        public void End(CodeNode node)
        {
            if (VariableCount == 0 && ClobberedRegs.IsEmpty) return;

            var vaIndex = new RegisterCount();
            vaIndex.IndexFrom(RegCount);

            var vaMap = new VariableMap(VariableCount);

            vaMap.Count.CopyFrom(RegCount);
            vaMap.Start.CopyFrom(vaIndex);
            vaMap.InRegs.CopyFrom(InRegs);
            vaMap.OutRegs.CopyFrom(OutRegs);
            vaMap.ClobberedRegs.CopyFrom(ClobberedRegs);

            var vi = 0;
            while (VariableCount != 0)
            {
                var va = _tmpList[vi];
                var vd = va.VariableData;

                var rClass = vd.Info.RegisterClass;
                var dstIndex = vaIndex.Get(rClass);

                vaIndex.Add(rClass);

                if (va.InRegs != 0) va.AllocableRegs = va.InRegs;
                else if (va.OutRegIndex != RegisterIndex.Invalid) va.AllocableRegs = Utils.Mask(va.OutRegIndex);
                else va.AllocableRegs &= ~InRegs.Get(rClass);

                vd.Attributes = null;
                vaMap.Attributes[dstIndex].CopyFrom(va);
                vi++;
                VariableCount--;
            }
            node.VariableMap = vaMap;
            return;
        }

        public void RegisterContextVariable(VariableData vd)
        {
            if (vd.LocalId != Constants.InvalidId) return;

            vd.LocalId = ContextVd.Count;
            ContextVd.Add(vd);
        }
    }
}