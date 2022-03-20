using System;
using System.Linq;
using AsmJit.Common;
using AsmJit.Common.Extensions;

namespace AsmJit.AssemblerContext
{
    public sealed class Assembler : AssemblerBase
    {
        private CodeContext _codeContext;

        public static CodeContext<T> CreateContext<T>()
        {
            var t = typeof(T);

            var args = new Type[0];
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
                var ret = gargs.Last();
                delType = DelegateCreator.NewDelegateType(ret, args);
            }
            else throw new ArgumentException(string.Format("unknown type:{0}", t));

            var asm = new Assembler();
            var ctx = new CodeContext<T>(asm, delType);
            asm._codeContext = ctx;
            return ctx;
        }
    }
}
