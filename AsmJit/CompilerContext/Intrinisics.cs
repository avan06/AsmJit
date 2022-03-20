using AsmJit.Common.Enums;
using AsmJit.Common.Operands;

namespace AsmJit.CompilerContext
{
    public partial class CodeContext
    {
        public class Intrinisics
        {
            private CodeContext c;

            public XmmVariable _mm_add_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Addss, a,b);
                return a;
            }

            public XmmVariable _mm_add_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Addps, a, b);
                return a;
            }

            public XmmVariable _mm_sub_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Subss, a, b);
                return a;
            }

            public XmmVariable _mm_sub_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Subps, a, b);
                return a;
            }

            public XmmVariable _mm_mul_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Mulss, a, b);
                return a;
            }

            public XmmVariable _mm_mul_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Mulps, a, b);
                return a;
            }

            public XmmVariable _mm_div_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Divss, a, b);
                return a;
            }

            public XmmVariable _mm_div_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Divps, a, b);
                return a;
            }

            public XmmVariable _mm_sqrt_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Sqrtss, r, a);
                return r;
            }

            public XmmVariable _mm_sqrt_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Sqrtps, r, a);
                return r;
            }

            public XmmVariable _mm_rcp_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Rcpss, r, a);
                return r;
            }

            public XmmVariable _mm_rcp_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Rcpps, r, a);
                return r;
            }

            public XmmVariable _mm_rsqrt_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Rsqrtss, r, a);
                return r;
            }

            public XmmVariable _mm_rsqrt_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(InstructionId.Rsqrtps, r, a);
                return r;
            }

            public XmmVariable _mm_min_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Minss, a, b);
                return a;
            }

            public XmmVariable _mm_min_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Minps, a, b);
                return a;
            }

            public XmmVariable _mm_max_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Maxss, a, b);
                return a;
            }

            public XmmVariable _mm_max_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Maxps, a, b);
                return a;
            }

            public XmmVariable _mm_and_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Andps, a, b);
                return a;
            }

            public XmmVariable _mm_andnot_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Andnps, a, b);
                return a;
            }

            public XmmVariable _mm_or_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Orps, a, b);
                return a;
            }

            public XmmVariable _mm_xor_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(InstructionId.Xorps, a, b);
                return a;
            }
        }
    }
}
