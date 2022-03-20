using System;

namespace AsmJit.Common.Extensions
{
    public static class EnumExtensions
    {
        public static bool IsSet(this Enum value, Enum flag) => (Convert.ToInt64(value) & Convert.ToInt64(flag)) != 0;
    }
}