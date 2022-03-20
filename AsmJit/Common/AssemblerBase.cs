using System;
using System.Collections.Generic;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;

namespace AsmJit.Common
{
    public class AssemblerBase
    {
        private int _id; 
        private static int _idGenerator;
        private CodeBuffer _codeBuffer;
        private List<LabelData> _labels = new List<LabelData>();
        private List<DataBlock> _data = new List<DataBlock>();
        private AssemblerFeatures _features;
        private InstructionOptions _instructionOptions;
        internal Pointer BaseAddress;// = Pointer.Invalid;

        internal AssemblerBase()
        {
            _codeBuffer = new CodeBuffer(this);
            _id = _idGenerator++;
            _features |= AssemblerFeatures.OptimizedAlign;
            _instructionOptions = InstructionOptions.None;
        }

        internal bool HasFeature(AssemblerFeatures feature) => _features.IsSet(feature);

        internal InstructionOptions GetInstructionOptionsAndReset()
        {
            var options = _instructionOptions;
            _instructionOptions = InstructionOptions.None;
            return options;
        }

        public void Data(Label label, int alignment, params Data[] data) => _data.Add(new DataBlock(label, alignment, data));

        internal LabelData CreateLabelData(out int id)
        {
            var data = new LabelData(_id);
            id = _labels.Count;
            _labels.Add(data);
            return data;
        }

        internal LabelData GetLabelData(long id) => _labels[(int)id];

        internal void Unfollow() => _instructionOptions |= InstructionOptions.Unfollow;

        internal Label CreateLabel()
        {
            CreateLabelData(out int id);
            return new Label(id);
        }

        internal void Embed(Pointer data, int size) => _codeBuffer.Embed(data, size);

        internal void Align(AligningMode alignMode, int offset) => _codeBuffer.Align(alignMode, offset);

        internal void Bind(int labelId) => _codeBuffer.Bind(labelId);

        internal Pointer Make()
        {
            unsafe
            {
                foreach (DataBlock dataItem in _data)
                {
                    if (dataItem.Label == null) throw new ArgumentException("DataBlock label is null");
                    Align(AligningMode.Data, dataItem.Alignment);
                    Bind(dataItem.Label.Id);
                    foreach (Data v in dataItem.Data)
                    {
                        fixed (byte* pv = v.ByteData)
                        {
                            Embed(pv, v.ByteData.Length);
                        }
                    }
                }
            }
            return _codeBuffer.Make();
        }

        internal Pointer Make(out int codeSize) => _codeBuffer.Make(out codeSize);

        internal void Emit(InstructionId instructionId, params Operand[] ops) => _codeBuffer.Emit(instructionId, _instructionOptions, ops);

        internal void Emit(InstructionId instructionId, InstructionOptions options, Operand o0 = null, Operand o1 = null, Operand o2 = null, Operand o3 = null)
        {
            _instructionOptions = options;
            Emit(instructionId, o0, o1, o2, o3);
        }
    }
}
