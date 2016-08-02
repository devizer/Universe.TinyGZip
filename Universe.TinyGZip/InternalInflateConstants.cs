namespace Universe.TinyGZip.InternalImplementation
{
    internal static class InternalInflateConstants
    {
        internal static readonly int[] InflateMask = new int[17]
        {
            0,
            1,
            3,
            7,
            15,
            31,
            63,
            sbyte.MaxValue,
            byte.MaxValue,
            511,
            1023,
            2047,
            4095,
            8191,
            16383,
            short.MaxValue,
            ushort.MaxValue
        };
    }
}