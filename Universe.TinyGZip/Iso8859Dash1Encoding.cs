// Decompiled with JetBrains decompiler
// Type: Ionic.Encoding.Iso8859Dash1Encoding
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;
    using System.Text;

    public class Iso8859Dash1Encoding : Encoding
    {
        public override string WebName
        {
            get { return "iso-8859-1"; }
        }

        public static int CharacterCount
        {
            get { return 256; }
        }

        public override int GetBytes(char[] chars, int start, int count, byte[] bytes, int byteIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "null array");
            if (bytes == null)
                throw new ArgumentNullException("bytes", "null array");
            if (start < 0)
                throw new ArgumentOutOfRangeException("start");
            if (count < 0)
                throw new ArgumentOutOfRangeException("charCount");
            if (chars.Length - start < count)
                throw new ArgumentOutOfRangeException("chars");
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex");
            for (var index = 0; index < count; ++index)
            {
                var ch = chars[start + index];
                bytes[byteIndex + index] = (int) ch < (int) byte.MaxValue ? (byte) ch : (byte) 63;
            }
            return count;
        }

        public override int GetChars(byte[] bytes, int start, int count, char[] chars, int charIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "null array");
            if (bytes == null)
                throw new ArgumentNullException("bytes", "null array");
            if (start < 0)
                throw new ArgumentOutOfRangeException("start");
            if (count < 0)
                throw new ArgumentOutOfRangeException("charCount");
            if (bytes.Length - start < count)
                throw new ArgumentOutOfRangeException("bytes");
            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException("charIndex");
            for (var index = 0; index < count; ++index)
                chars[charIndex + index] = (char) bytes[index + start];
            return count;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return count;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }
    }
}