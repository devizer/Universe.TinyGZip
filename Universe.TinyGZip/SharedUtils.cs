namespace Universe.TinyGZip.InternalImplementation
{
    using System.IO;
    using System.Text;

    internal class SharedUtils
    {
        public static int URShift(int number, int bits)
        {
            return (int) ((uint) number >> bits);
        }

        public static int ReadInput(TextReader sourceTextReader, byte[] target, int start, int count)
        {
            if (target.Length == 0)
                return 0;
            var buffer = new char[target.Length];
            var num = sourceTextReader.Read(buffer, start, count);
            if (num == 0)
                return -1;
            for (var index = start; index < start + num; ++index)
                target[index] = (byte) buffer[index];
            return num;
        }

        internal static byte[] ToByteArray(string sourceString)
        {
            return Encoding.UTF8.GetBytes(sourceString);
        }

        internal static char[] ToCharArray(byte[] byteArray)
        {
            return Encoding.UTF8.GetChars(byteArray);
        }
    }
}