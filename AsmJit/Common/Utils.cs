﻿using System;
using System.Collections.Generic;
using AsmJit.Common.Extensions;

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

        /// <summary>
        /// Creates a shuffle immediate value that be used with SSE/AVX/AVX-512 instructions to shuffle 4 elements in a vector.
        /// 
        /// Shuffle constants can be used to encode an immediate for these instructions:
        ///   - `pshufw`
        ///   - `pshuflw|vpshuflw`
        ///   - `pshufhw|vpshufhw`
        ///   - `pshufd|vpshufd`
        ///   - `shufps|vshufps`
        /// </summary>
        /// <param name="a">Position of the first  component [0, 3].</param>
        /// <param name="b">Position of the second component [0, 3].</param>
        /// <param name="c">Position of the third  component [0, 3].</param>
        /// <param name="d">Position of the fourth component [0, 3].</param>
        /// <returns></returns>
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

        internal static int Mask(params int[] xs)
        {
            if (xs == null || xs.Length == 0 || xs.Length > 10) throw new ArgumentException();

            int result = 0;
            for (int i = 0; i < xs.Length; i++) result |= Mask(xs[i]);
            return result;
        }

        internal static long Bits(int x)
        {
            var overflow = -(x >= sizeof(long) * 8).AsInt();
            return ((1 << x) - 1L) | overflow;
        }
    }
}
