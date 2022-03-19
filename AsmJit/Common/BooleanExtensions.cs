namespace AsmJit.Common
{
    public static class BooleanExtensions
    {
        public static int AsInt(this bool v) => !v ? 0 : 1;

        public static byte AsByte(this bool v) => (byte)(!v ? 0 : 1);

        public static uint AsUInt(this bool v) => !v ? 0u : 1u;
    }
}