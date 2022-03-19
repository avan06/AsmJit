using System;
using System.Collections.Generic;

namespace AsmJit.Common
{
    public static class Utils
    {
        internal static readonly HashSet<Type> Actions = new HashSet<Type>
        {
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
            typeof(Action<,,,,>),
            typeof(Action<,,,,,>),
            typeof(Action<,,,,,,>),
            typeof(Action<,,,,,,,>),
            typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,,>)
        };

        internal static readonly HashSet<Type> Funcs = new HashSet<Type>
        {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>)
        };

        internal static int ArchIndex(int total, int index) => BitConverter.IsLittleEndian ? index : total - 1 - index;

        public static int Shuffle(uint a, uint b, uint c, uint d)
        {
            if (a > 0x3 || b > 0x3 || c > 0x3 || d > 0x3) throw new ArgumentException();
            var result = (a << 6) | (b << 4) | (c << 2) | d;
            return (int)result;
        }

        internal static int Mask(int x)
        {
            if (x >= 32) throw new ArgumentException();
            return 1 << x;
        }

        internal static int Mask(int x0, int x1) => Mask(x0) | Mask(x1);

        internal static int Mask(int x0, int x1, int x2) => Mask(x0) | Mask(x1) | Mask(x2);

        internal static int Mask(int x0, int x1, int x2, int x3) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3);

        internal static int Mask(int x0, int x1, int x2, int x3, int x4) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4);

        internal static int Mask(int x0, int x1, int x2, int x3, int x4, int x5) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4) | Mask(x5);

        internal static int Mask(int x0, int x1, int x2, int x3, int x4, int x5, int x6) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4) | Mask(x5) | Mask(x6);

        public static int Mask(int x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4) | Mask(x5) | Mask(x6) | Mask(x7);

        internal static int Mask(int x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4) | Mask(x5) | Mask(x6) | Mask(x7) | Mask(x8);

        internal static int Mask(int x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9) => Mask(x0) | Mask(x1) | Mask(x2) | Mask(x3) | Mask(x4) | Mask(x5) | Mask(x6) | Mask(x7) | Mask(x8) | Mask(x9);

        internal static long Bits(int x)
        {
            var overflow = -(x >= sizeof(long) * 8).AsInt();
            return ((1 << x) - 1L) | overflow;
        }
    }
}
