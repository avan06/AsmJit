using System;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;

namespace AsmJit.Common.Variables
{
    internal sealed class VariableMap
    {
        public readonly RegisterCount Count;

        public readonly RegisterMask InRegs;

        public readonly RegisterMask OutRegs;

        public readonly RegisterMask ClobberedRegs;

        public readonly RegisterCount Start;

        public readonly int AttributesCount;

        public readonly VariableAttributes[] Attributes;

        public VariableMap(int attrCount)
        {
            InRegs = new RegisterMask();
            OutRegs = new RegisterMask();
            ClobberedRegs = new RegisterMask();
            Start = new RegisterCount();
            Count = new RegisterCount();
            AttributesCount = attrCount;
            Attributes = new VariableAttributes[attrCount].InitializeWith(() => new VariableAttributes());
        }

        public VariableAttributes FindAttributes(VariableData vd)
        {
            var list = Attributes;
            var count = AttributesCount;
            for (var i = 0; i < count; i++)
            {
                if (list[i].VariableData == vd) return list[i];
            }
            return null;
        }

        public VariableAttributes FindAttributesByClass(RegisterClass rClass, VariableData vd)
        {
            var list = GetListByClass(rClass);
            var count = Count.Get(rClass);
            for (var i = 0; i < count; i++)
            {
                if (list[i].VariableData == vd) return list[i];
            }
            return null;
        }

        public VariableAttributes[] GetListByClass(RegisterClass rClass)
        {
            var tmp = new VariableAttributes[AttributesCount - Start.Get(rClass)];
            Array.Copy(Attributes, Start.Get(rClass), tmp, 0, AttributesCount - Start.Get(rClass));
            return tmp;
        }
    }
}