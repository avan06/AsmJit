﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsmJit.Common;

namespace AsmJit.CompilerContext.CodeTree
{
    internal static class CodeNodeExtensions
    {
        internal static bool IsFetched(this CodeNode node) => node.FlowId != 0;

        internal static bool IsRemovable(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Removable);

        internal static bool IsInformative(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Informative);

        internal static bool IsTranslated(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Translated);

        internal static bool IsScheduled(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Scheduled);

        internal static bool IsRet(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Ret);

        internal static bool IsJmp(this CodeNode node) => node.Flags.IsSet(CodeNodeFlags.Jmp);

        internal static bool IsJmpOrJcc(this CodeNode node) => node.IsJmp() || node.Flags.IsSet(CodeNodeFlags.Jcc);
    }
}
