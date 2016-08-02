// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.ZlibConstants
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    public static class ZlibConstants
    {
        public const int WindowBitsMax = 15;
        public const int WindowBitsDefault = 15;
        public const int Z_OK = 0;
        public const int Z_STREAM_END = 1;
        public const int Z_NEED_DICT = 2;
        public const int Z_STREAM_ERROR = -2;
        public const int Z_DATA_ERROR = -3;
        public const int Z_BUF_ERROR = -5;
        public const int WorkingBufferSizeDefault = 16384;
        public const int WorkingBufferSizeMin = 1024;
    }
}