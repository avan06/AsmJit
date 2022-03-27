﻿using System;
using AsmJit.Common;
using AsmJit.Common.Enums;
using AsmJit.Common.Extensions;
using AsmJit.Common.Operands;
using AsmJit.Common.Variables;
using AsmJit.CompilerContext.CodeTree;

namespace AsmJit.CompilerContext
{
    internal sealed class CallAllocator : Allocator
    {
        private RegisterMask _willAlloc = new RegisterMask();
        private RegisterMask _willSpill = new RegisterMask();

        public CallAllocator(Compiler compiler, CodeContext codeContext, Translator translator, VariableContext ctx) : base(compiler, codeContext, translator, ctx) { }

        private void InitImpl(CallNode node, VariableMap map)
        {
            Init(node, map);

            _willAlloc.CopyFrom(node.UsedArgs);
            _willSpill.Reset();
        }

        public void Run(CallNode node)
        {
            var map = node.VariableMap;
            if (map == null) return;

            // Initialize the allocator; prepare basics and connect Vd->Va.
            InitImpl(node, map);

            // Plan register allocation. Planner is only able to assign one register per
            // variable. If any variable is used multiple times it will be handled later.
            Plan(RegisterClass.Gp);
            Plan(RegisterClass.Mm);
            Plan(RegisterClass.Xyz);

            // Spill.
            Spill(RegisterClass.Gp);
            Spill(RegisterClass.Mm);
            Spill(RegisterClass.Xyz);

            // Alloc.
            Alloc(RegisterClass.Gp);
            Alloc(RegisterClass.Mm);
            Alloc(RegisterClass.Xyz);

            // Unuse clobbered registers that are not used to pass function arguments and
            // save variables used to pass function arguments that will be reused later on.
            Save(RegisterClass.Gp);
            Save(RegisterClass.Mm);
            Save(RegisterClass.Xyz);

            // Allocate immediates in registers and on the stack.
            AllocImmsOnStack();

            // Duplicate.
            Duplicate(RegisterClass.Gp);
            Duplicate(RegisterClass.Mm);
            Duplicate(RegisterClass.Xyz);

            var ops = new[] { node.Target };
            Translator.TranslateOperands(ops);
            node.Target = ops[0];

            Compiler.SetCurrentNode(node);

            var decl = node.FunctionDeclaration;
            if (decl.CalleePopsStack && decl.ArgumentsStackSize != 0) Compiler.Emit(InstructionId.Sub, Cpu.Zsp, (Immediate)decl.ArgumentsStackSize);

            // Clobber.
            Clobber(RegisterClass.Gp);
            Clobber(RegisterClass.Mm);
            Clobber(RegisterClass.Xyz);

            // Return.
            Return();

            // Unuse.
            UnuseAfter(RegisterClass.Gp);
            UnuseAfter(RegisterClass.Mm);
            UnuseAfter(RegisterClass.Xyz);

            // Cleanup; disconnect Vd->Va.
            Cleanup();
        }

        private void Plan(RegisterClass rClass)
        {
            int i;
            var clobbered = Map.ClobberedRegs.Get(rClass);

            var willAlloc = _willAlloc.Get(rClass);
            var willFree = clobbered & ~willAlloc;

            var list = VaList[(int)rClass];
            var count = Count.Get(rClass);

            var state = VariableContext.State;
            // Calculate 'willAlloc' and 'willFree' masks based on mandatory masks.
            for (i = 0; i < count; i++)
            {
                var va = list[i];
                var vd = va.VariableData;

                var vaFlags = va.Flags;
                var regIndex = vd.RegisterIndex;
                var regMask = regIndex != RegisterIndex.Invalid ? Utils.Mask(regIndex) : 0;

                if (vaFlags.IsSet(VariableFlags.RReg))
                {
                    // Planning register allocation. First check whether the variable is
                    // already allocated in register and if it can stay there. Function
                    // arguments are passed either in a specific register or in stack so
                    // we care mostly of mandatory registers.
                    var inRegs = va.InRegs;

                    if (inRegs == 0) inRegs = va.AllocableRegs;

                    // Optimize situation where the variable has to be allocated in a
                    // mandatory register, but it's already allocated in register that
                    // is not clobbered (i.e. it will survive function call).
                    if ((regMask & inRegs) != 0 || ((regMask & ~clobbered) != 0 && !vaFlags.IsSet(VariableFlags.Unuse)))
                    {
                        va.InRegIndex = regIndex;
                        va.Flags |= VariableFlags.AllocRDone;
                        Done.Add(rClass);
                    }
                    else willFree |= regMask;
                }
                else
                {
                    // Memory access - if variable is allocated it has to be freed.
                    if (regMask != 0) willFree |= regMask;
                    else
                    {
                        va.Flags |= VariableFlags.AllocRDone;
                        Done.Add(rClass);
                    }
                }
            }

            // Occupied registers without 'willFree' registers; contains basically
            // all the registers we can use to allocate variables without inRegs
            // speficied.
            var occupied = state.Occupied.Get(rClass) & ~willFree;
            var willSpill = 0;

            // Find the best registers for variables that are not allocated yet. Only
            // useful for Gp registers used as call operand.
            for (i = 0; i < count; i++)
            {
                var va = list[i];
                var vd = va.VariableData;

                var vaFlags = va.Flags;
                if (vaFlags.IsSet(VariableFlags.AllocRDone) || !vaFlags.IsSet(VariableFlags.RReg)) continue;

                // All registers except Gp used by call itself must have inRegIndex.
                var m = va.InRegs;
                if (rClass != RegisterClass.Gp || m != 0)
                {
                    if (!(m != 0)) { throw new ArgumentException(); }
                    va.InRegIndex = m.FindFirstBit();
                    willSpill |= occupied & m;
                    continue;
                }

                m = va.AllocableRegs & ~(willAlloc ^ m);
                m = GuessAlloc(rClass, vd, m);
                if (!(m != 0)) { throw new ArgumentException(); }

                var candidateRegs = m & ~occupied;
                if (candidateRegs == 0)
                {
                    candidateRegs = m & occupied & ~state.Modified.Get(rClass);
                    if (candidateRegs == 0) candidateRegs = m;
                }

                if (!vaFlags.IsSet(VariableFlags.WReg) && !vaFlags.IsSet(VariableFlags.Unuse) && candidateRegs.IsSet(~clobbered)) candidateRegs &= ~clobbered;

                var regIndex = candidateRegs.FindFirstBit();
                var regMask = Utils.Mask(regIndex);

                va.InRegIndex = regIndex;
                va.InRegs = regMask;

                willAlloc |= regMask;
                willSpill |= regMask & occupied;
                willFree &= ~regMask;

                occupied |= regMask;
            }

            // Set calculated masks back to the allocator; needed by spill() and alloc().
            _willSpill.Set(rClass, willSpill);
            _willAlloc.Set(rClass, willAlloc);
        }

        private void Spill(RegisterClass rClass)
        {
            var m = _willSpill.Get(rClass);
            var i = -1;

            if (m == 0) return;

            var state = VariableContext.State;
            var sVars = state.GetListByClass(rClass);

            // Available registers for decision if move has any benefit over spill.
            var availableRegs = VariableContext.GaRegs[rClass] & ~(state.Occupied.Get(rClass) | m | _willAlloc.Get(rClass));

            do
            {
                // We always advance one more to destroy the bit that we have found.
                var bitIndex = m.FindFirstBit() + 1;

                i += bitIndex;
                m >>= bitIndex;

                var vd = sVars[i];
                if (!(vd != null)) throw new ArgumentException();
                if (!(vd.Attributes == null)) throw new ArgumentException();

                if (vd.IsModified && availableRegs != 0)
                {
                    var available = GuessSpill(rClass, vd, availableRegs);
                    if (available != 0)
                    {
                        var regIndex = available.FindFirstBit();
                        var regMask = Utils.Mask(regIndex);

                        Translator.Move(vd, rClass, regIndex);
                        availableRegs ^= regMask;
                        continue;
                    }
                }

                Translator.Spill(vd, rClass);
            } while (m != 0);
        }

        private void Alloc(RegisterClass rClass)
        {
            if (Done.Get(rClass) == Count.Get(rClass)) return;
            var list = VaList[(int)rClass];
            var count = Count.Get(rClass);

            var state = VariableContext.State;
            //var sVars = state.GetListByClass(rClass);

            bool didWork;

            do
            {
                didWork = false;
                int i;
                for (i = 0; i < count; i++)
                {
                    var aVa = list[i];
                    var aVd = aVa.VariableData;

                    if ((aVa.Flags & (VariableFlags.RReg | VariableFlags.AllocRDone)) != VariableFlags.RReg) continue;

                    var aIndex = aVd.RegisterIndex;
                    var bIndex = aVa.InRegIndex;

                    // Shouldn't be the same.
                    if (aIndex == bIndex) throw new ArgumentException();

                    var bVd = state.GetListByClass(rClass)[bIndex];
                    if (bVd != null)
                    {
                        var bVa = bVd.Attributes;

                        // Gp registers only - Swap two registers if we can solve two
                        // allocation tasks by a single 'xchg' instruction, swapping
                        // two registers required by the instruction/node or one register
                        // required with another non-required.
                        if (rClass != RegisterClass.Gp) continue;
                        Translator.SwapGp(aVd, bVd);

                        aVa.Flags |= VariableFlags.AllocRDone;
                        Done.Add(rClass);

                        // Doublehit, two registers allocated by a single swap.
                        if (bVa != null && bVa.InRegIndex == aIndex)
                        {
                            bVa.Flags |= VariableFlags.AllocRDone;
                            Done.Add(rClass);
                        }

                        didWork = true;
                    }
                    else if (aIndex != RegisterIndex.Invalid)
                    {
                        Translator.Move(aVd, rClass, bIndex);
                        VariableContext.ClobberedRegs.Or(rClass, Utils.Mask(bIndex));

                        aVa.Flags |= VariableFlags.AllocRDone;
                        Done.Add(rClass);

                        didWork = true;
                    }
                    else
                    {
                        Translator.Alloc(aVd, rClass, bIndex);
                        VariableContext.ClobberedRegs.Or(rClass, Utils.Mask(bIndex));

                        aVa.Flags |= VariableFlags.AllocRDone;
                        Done.Add(rClass);

                        didWork = true;
                    }
                }
            }
            while (didWork);
        }

        private void Duplicate(RegisterClass rClass)
        {
            var list = VaList[(int)rClass];
            var count = Count.Get(rClass);

            for (var i = 0; i < count; i++)
            {
                var va = list[i];
                if (!va.Flags.IsSet(VariableFlags.RReg)) continue;

                var inRegs = va.InRegs;
                if (inRegs == 0) continue;

                var vd = va.VariableData;
                var regIndex = vd.RegisterIndex;

                if (regIndex == RegisterIndex.Invalid) throw new ArgumentException();

                inRegs &= ~Utils.Mask(regIndex);
                if (inRegs == 0) continue;

                for (var dupIndex = 0; inRegs != 0; dupIndex++, inRegs >>= 1)
                {
                    if (inRegs.IsSet(0x1))
                    {
                        Translator.EmitMove(vd, dupIndex, regIndex);
                        VariableContext.ClobberedRegs.Or(rClass, Utils.Mask(dupIndex));
                    }
                }
            }
        }

        private void Save(RegisterClass rClass)
        {
            var state = VariableContext.State;
            var sVars = state.GetListByClass(rClass);

            int i;
            var affected = Map.ClobberedRegs.Get(rClass) & state.Occupied.Get(rClass) & state.Modified.Get(rClass);

            for (i = 0; affected != 0; i++, affected >>= 1)
            {
                if (!affected.IsSet(0x1)) continue;
                var vd = sVars[i];
                if (vd == null || !vd.IsModified) throw new ArgumentException();

                var va = vd.Attributes;
                if (va == null || (va.Flags & (VariableFlags.WReg | VariableFlags.Unuse)) == 0) Translator.Save(vd, rClass);
            }
        }

        private void AllocImmsOnStack()
        {
            var node = Node.As<CallNode>();
            var decl = node.FunctionDeclaration;

            var argCount = decl.ArgumentCount;
            var args = node.Arguments;

            for (var i = 0; i < argCount; i++)
            {
                var op = args[i];

                if (!op.IsImmedate()) continue;

                var imm = op.As<Immediate>();
                var arg = decl.GetArgument(i);
                var varType = arg.VariableType;

                if (arg.StackOffset != Constants.InvalidValue)
                {
                    var dst = new Memory(Cpu.Zsp, -Cpu.Info.RegisterSize + arg.StackOffset);
                    Translator.EmitMoveImmOnStack(varType, dst, imm);
                }
                else Translator.EmitMoveImmToReg(varType, arg.RegisterIndex, imm);
            }
        }

        private void Clobber(RegisterClass rClass)
        {
            var state = VariableContext.State;
            var sVars = state.GetListByClass(rClass);

            int i;
            var affected = Map.ClobberedRegs.Get(rClass) & state.Occupied.Get(rClass);

            for (i = 0; affected != 0; i++, affected >>= 1)
            {
                if (!affected.IsSet(0x1)) continue;
                var vd = sVars[i];
                if (vd == null) throw new ArgumentException();

                var va = vd.Attributes;
                var vdState = VariableUsage.None;

                if (!vd.IsModified || (va != null && (va.Flags & (VariableFlags.WAll | VariableFlags.Unuse)) != 0)) vdState = VariableUsage.Mem;

                Translator.Unuse(vd, rClass, vdState);
            }
        }

        private void Return()
        {
            var node = Node.As<CallNode>();
            var decl = node.FunctionDeclaration;

            int i;
            var rets = node.Return;

            for (i = 0; i < 2; i++)
            {
                var ret = decl.GetReturn(i);
                var op = rets[i];

                if (op.IsInvalid() || ret.RegisterIndex == RegisterIndex.Invalid || !op.IsVariable()) continue;

                var vd = Compiler.GetVariableData(op.Id);
                var vf = vd.Type.GetVariableInfo().ValueFlags;
                var regIndex = ret.RegisterIndex;

                switch (vd.Info.RegisterClass)
                {
                    case RegisterClass.Gp:
                        if (ret.VariableType.GetRegisterClass() != vd.Info.RegisterClass) throw new ArgumentException();

                        Translator.Unuse(vd, RegisterClass.Gp);
                        Translator.Attach(vd, RegisterClass.Gp, regIndex, true);
                        break;

                    case RegisterClass.Mm:
                        if (ret.VariableType.GetRegisterClass() != vd.Info.RegisterClass) throw new ArgumentException();

                        Translator.Unuse(vd, RegisterClass.Mm);
                        Translator.Attach(vd, RegisterClass.Mm, regIndex, true);
                        break;

                    case RegisterClass.Xyz:
                        if (ret.VariableType == VariableType.Fp32 || ret.VariableType == VariableType.Fp64)
                        {
                            var m = new Memory(Translator.GetVarMem(vd), (vf & VariableValueFlags.Sp) != 0 ? 4 : (vf & VariableValueFlags.Dp) != 0 ? 8 : ret.VariableType == VariableType.Fp32 ? 4 : 8);
                            Translator.Unuse(vd, RegisterClass.Xyz, VariableUsage.Mem);
                            CodeContext.Emit(InstructionId.Fstp, m);
                        }
                        else
                        {
                            if (ret.VariableType.GetRegisterClass() != vd.Info.RegisterClass) throw new ArgumentException();

                            Translator.Unuse(vd, RegisterClass.Xyz);
                            Translator.Attach(vd, RegisterClass.Xyz, regIndex, true);
                        }
                        break;
                }
            }
        }

        private void UnuseAfter(RegisterClass rClass)
        {
            var list = VaList[(int)rClass];
            var count = Count.Get(rClass);

            for (var i = 0; i < count; i++)
            {
                var va = list[i];

                if ((va.Flags & VariableFlags.Unuse) != 0) Translator.Unuse(va.VariableData, rClass);
            }
        }

        private int GuessAlloc(RegisterClass rClass, VariableData vd, int allocableRegs)
        {
            if (allocableRegs == 0) throw new ArgumentException();
            if (allocableRegs.IsPowerOf2()) return allocableRegs;

            var safeRegs = allocableRegs;
            var node = Node;

            for (var j = 0; j < MaxLookAhead; j++)
            {
                if (node.Flags.IsSet(CodeNodeFlags.Ret) || node.Flags.IsSet(CodeNodeFlags.Jcc)) break;
                if (node.Flags.IsSet(CodeNodeFlags.Jmp))
                {
                    node = node.As<JumpNode>().Target;
                    if (node == null) break;
                }
                node = node.Next;
                if (node == null) throw new ArgumentException();
                var map = node.VariableMap;
                if (map == null) continue;
                var va = map.FindAttributesByClass(rClass, vd);
                if (va != null)
                {
                    var inRegs = va.InRegs;
                    if (inRegs != 0)
                    {
                        safeRegs = allocableRegs;
                        allocableRegs &= inRegs;

                        return allocableRegs == 0 ? safeRegs : allocableRegs;
                    }
                }

                safeRegs = allocableRegs;
                allocableRegs &= ~(map.InRegs.Get(rClass) | map.OutRegs.Get(rClass) | map.ClobberedRegs.Get(rClass));

                if (allocableRegs == 0) break;
            }
            return safeRegs;
        }

        private int GuessSpill(RegisterClass rClass, VariableData vd, int allocableRegs)
        {
            if (allocableRegs == 0) throw new ArgumentException();
            return 0;
        }
    }
}
