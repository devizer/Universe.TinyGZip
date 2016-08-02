// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.WorkItem
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    internal class WorkItem
    {
        public byte[] buffer;
        public byte[] compressed;
        public int compressedBytesAvailable;
        public ZlibCodec compressor;
        public int crc;
        public int index;
        public int inputBytesAvailable;
        public int ordinal;

        public WorkItem(int size, CompressionLevel compressLevel, CompressionStrategy strategy, int ix)
        {
            buffer = new byte[size];
            compressed = new byte[size + (size/32768 + 1)*5*2];
            compressor = new ZlibCodec();
            compressor.InitializeDeflate(compressLevel, false);
            compressor.OutputBuffer = compressed;
            compressor.InputBuffer = buffer;
            index = ix;
        }
    }
}