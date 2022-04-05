using System;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Operands;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    internal sealed class CodeProcessor
    {
        private AssemblerBase _assemblerBase;
        private Compiler _compiler;
        private CodeContext _codeContext;

        internal CodeProcessor(AssemblerBase assemblerBase, Compiler compiler, CodeContext codeContext)
        {
            _assemblerBase = assemblerBase;
            _compiler = compiler;
            _codeContext = codeContext;
        }

        internal void FetchAndTranslate(FunctionNode func)
        {
            var fetcher = new Fetcher(_compiler, func);
            var vc = fetcher.Run();
            var analyzer = new LivenessAnalyzer(func, vc);
            analyzer.Run();
            var translator = new Translator(_assemblerBase, _compiler, _codeContext, func, vc);
            translator.Run();
        }

        internal void Serialize(CodeNode start, CodeNode stop)
        {
            var current = start;
            do
            {
                switch (current.Type)
                {
                    case CodeNodeType.None:
                        break;
                    case CodeNodeType.Instruction:
                        var node = current.As<InstructionNode>();
                        var instId = node.InstructionId;
                        var opList = node.Operands;
                        var opCount = node.Operands.Length;
                        Operand o0 = null;
                        Operand o1 = null;
                        Operand o2 = null;
                        Operand o3 = null;
                        if (node.IsSpecial())
                        {
                            switch (instId)
                            {
                                case var value when value == Inst.Cpuid:
                                    break;

                                case var value when
                                value == Inst.Cbw  ||
                                value == Inst.Cdq  ||
                                value == Inst.Cdqe ||
                                value == Inst.Cwd  ||
                                value == Inst.Cwde ||
                                value == Inst.Cqo:
                                    break;

                                case var value when value == Inst.Cmpxchg:
                                    o0 = opList[1];
                                    o1 = opList[2];
                                    break;

                                case var value when value == Inst.Cmpxchg8b || value == Inst.Cmpxchg16b:
                                    o0 = opList[4];
                                    break;

                                case var value when value == Inst.Daa || value == Inst.Das:
                                    break;

                                case var value when
                                value == Inst.Imul ||
                                value == Inst.Mul  ||
                                value == Inst.Idiv ||
                                value == Inst.Div:
                                    // Assume "Mul/Div dst_hi (implicit), dst_lo (implicit), src (explicit)".
                                    if (opCount != 3) throw new ArgumentException();
                                    o0 = opList[2];
                                    break;

                                case var value when value == Inst.MovPtr:
                                    break;

                                case var value when value == Inst.Lahf || value == Inst.Sahf:
                                    break;

                                case var value when value == Inst.Maskmovq || value == Inst.Maskmovdqu:
                                    o0 = opList[1];
                                    o1 = opList[2];
                                    break;

                                case var value when value == Inst.Enter:
                                    o0 = opList[0];
                                    o1 = opList[1];
                                    break;

                                case var value when value == Inst.Leave:
                                    break;

                                case var value when value == Inst.Ret:
                                    if (opCount > 0) o0 = opList[0];
                                    break;

                                case var value when value == Inst.Monitor || value == Inst.Mwait:
                                    break;

                                case var value when value == Inst.Pop:
                                    o0 = opList[0];
                                    break;

                                case var value when value == Inst.Popa || value == Inst.Popf:
                                    break;

                                case var value when value == Inst.Push:
                                    o0 = opList[0];
                                    break;

                                case var value when value == Inst.Pusha || value == Inst.Pushf:
                                    break;

                                case var value when
                                value == Inst.Rcl ||
                                value == Inst.Rcr ||
                                value == Inst.Rol ||
                                value == Inst.Ror ||
                                value == Inst.Sal ||
                                value == Inst.Sar ||
                                value == Inst.Shl ||
                                value == Inst.Shr:
                                    o0 = opList[0];
                                    o1 = Cpu.Registers.Cl;
                                    break;

                                case var value when value == Inst.Shld || value == Inst.Shrd:
                                    o0 = opList[0];
                                    o1 = opList[1];
                                    o2 = Cpu.Registers.Cl;
                                    break;

                                case var value when value == Inst.Rdtsc || value == Inst.Rdtscp:
                                    break;

                                case var value when
                                value == Inst.RepLodsB   ||
                                value == Inst.RepLodsD   ||
                                value == Inst.RepLodsQ   ||
                                value == Inst.RepLodsW   ||
                                value == Inst.RepMovsB   ||
                                value == Inst.RepMovsD   ||
                                value == Inst.RepMovsQ   ||
                                value == Inst.RepMovsW   ||
                                value == Inst.RepStosB   ||
                                value == Inst.RepStosD   ||
                                value == Inst.RepStosQ   ||
                                value == Inst.RepStosW   ||
                                value == Inst.RepeCmpsB  ||
                                value == Inst.RepeCmpsD  ||
                                value == Inst.RepeCmpsQ  ||
                                value == Inst.RepeCmpsW  ||
                                value == Inst.RepeScasB  ||
                                value == Inst.RepeScasD  ||
                                value == Inst.RepeScasQ  ||
                                value == Inst.RepeScasW  ||
                                value == Inst.RepneCmpsB ||
                                value == Inst.RepneCmpsD ||
                                value == Inst.RepneCmpsQ ||
                                value == Inst.RepneCmpsW ||
                                value == Inst.RepneScasB ||
                                value == Inst.RepneScasD ||
                                value == Inst.RepneScasQ ||
                                value == Inst.RepneScasW:
                                    break;

                                case var value when
                                value == Inst.Xrstor   ||
                                value == Inst.Xrstor64 ||
                                value == Inst.Xsave    ||
                                value == Inst.Xsave64  ||
                                value == Inst.Xsaveopt ||
                                value == Inst.Xsaveopt64:
                                    o0 = opList[0];
                                    break;

                                case var value when value == Inst.Xgetbv || value == Inst.Xsetbv:
                                    break;

                                default:
                                    throw new ArgumentException();
                            }
                        }
                        else
                        {
                            if (opCount > 0) o0 = opList[0];
                            if (opCount > 1) o1 = opList[1];
                            if (opCount > 2) o2 = opList[2];
                            if (opCount > 3) o3 = opList[3];
                        }
                        _assemblerBase.Emit(instId, node.InstructionOptions, o0, o1, o2, o3);
                        break;
                    case CodeNodeType.Data:
                        var dnode = current.As<DataNode>();
                        _assemblerBase.Embed(dnode.Data, dnode.Size);
                        break;
                    case CodeNodeType.Alignment:
                        var anode = current.As<AlignNode>();
                        _assemblerBase.Align(anode.AlignMode, anode.Offset);
                        break;
                    case CodeNodeType.Label:
                        var lnode = current.As<LabelNode>();
                        _assemblerBase.Bind(lnode.LabelId);
                        break;
                    case CodeNodeType.Comment:
                    case CodeNodeType.Hint:
                    case CodeNodeType.Sentinel:
                    case CodeNodeType.Function:
                    case CodeNodeType.Return:
                        break;
                    case CodeNodeType.Call:
                        var clnode = current.As<CallNode>();
                        _assemblerBase.Emit(Inst.Call, InstructionOptions.None, clnode.Target);
                        break;
                    case CodeNodeType.CallArgument:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("CodeNodeType", current.Type, "Not must be reached");
                }
                current = current.Next;
            } while (current != stop);
        }
    }
}
