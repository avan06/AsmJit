using AsmJit.Common;
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
                c.Emit(Inst.Addss, a,b);
                return a;
            }

            public XmmVariable _mm_add_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Addps, a, b);
                return a;
            }

            public XmmVariable _mm_sub_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Subss, a, b);
                return a;
            }

            public XmmVariable _mm_sub_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Subps, a, b);
                return a;
            }

            public XmmVariable _mm_mul_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Mulss, a, b);
                return a;
            }

            public XmmVariable _mm_mul_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Mulps, a, b);
                return a;
            }

            public XmmVariable _mm_div_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Divss, a, b);
                return a;
            }

            public XmmVariable _mm_div_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Divps, a, b);
                return a;
            }

            public XmmVariable _mm_sqrt_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Sqrtss, r, a);
                return r;
            }

            public XmmVariable _mm_sqrt_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Sqrtps, r, a);
                return r;
            }

            public XmmVariable _mm_rcp_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Rcpss, r, a);
                return r;
            }

            public XmmVariable _mm_rcp_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Rcpps, r, a);
                return r;
            }

            public XmmVariable _mm_rsqrt_ss(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Rsqrtss, r, a);
                return r;
            }

            public XmmVariable _mm_rsqrt_ps(XmmVariable a)
            {
                var r = c.Xmm();
                c.Emit(Inst.Rsqrtps, r, a);
                return r;
            }

            public XmmVariable _mm_min_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Minss, a, b);
                return a;
            }

            public XmmVariable _mm_min_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Minps, a, b);
                return a;
            }

            public XmmVariable _mm_max_ss(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Maxss, a, b);
                return a;
            }

            public XmmVariable _mm_max_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Maxps, a, b);
                return a;
            }

            public XmmVariable _mm_and_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Andps, a, b);
                return a;
            }

            public XmmVariable _mm_andnot_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Andnps, a, b);
                return a;
            }

            public XmmVariable _mm_or_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Orps, a, b);
                return a;
            }

            public XmmVariable _mm_xor_ps(XmmVariable a, XmmVariable b)
            {
                c.Emit(Inst.Xorps, a, b);
                return a;
            }
        }
    }
}
