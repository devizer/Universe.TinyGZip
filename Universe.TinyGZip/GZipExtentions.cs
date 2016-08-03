namespace Universe.TinyGZip
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using SysGZip = System.IO.Compression;

    public class GZipExtentions
    {
        private static bool? _isSupported = null;
        static readonly object Sync = new object();
        static readonly string _notSupportedMessage = "System.IO.Compression.GZipStream does not support decompression.";

        public static Stream CreateDecompressor(Stream gzipped)
        {
            if (gzipped == null)
                throw new ArgumentNullException("gzipped");

            if (IsSystemGZipSupported)
                return new SysGZip.GZipStream(gzipped, SysGZip.CompressionMode.Decompress);
            else
                return new Universe.TinyGZip.GZipStream(gzipped, CompressionMode.Decompress, false);
        }

        public static bool IsSystemGZipSupported
        {
            get
            {
                if (!_isSupported.HasValue)
                    lock(Sync)
                        if (!_isSupported.HasValue)
                        {
                            // plain is {5,4,3,2,1}
                            byte[] gzipped = {
                                0x1f, 0x8b, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x63, 0x65,
                                0x61, 0x66, 0x62, 0x04, 0x00, 0x77, 0x03, 0xd7, 0xc6, 0x05, 0x00, 0x00, 0x00
                            };

                            try
                            {
                                MemoryStream mem = new MemoryStream(gzipped);
                                using (SysGZip.GZipStream s = new SysGZip.GZipStream(mem, SysGZip.CompressionMode.Decompress))
                                {
                                    byte[] plain = new byte[5 + 1];
                                    var n = s.Read(plain, 0, plain.Length);
                                    if (n != 5)
                                        throw new NotSupportedException(_notSupportedMessage);

                                    if (plain[0] != 5 || plain[1] != 4 || plain[2] != 3 || plain[3] != 2 || plain[4] != 1)
                                    {
                                        throw new NotSupportedException(_notSupportedMessage);
                                    }

                                    _isSupported = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _isSupported = false;
                                Trace.WriteLine(_notSupportedMessage + " We are using TinyGZip implementation" + Environment.NewLine + ex);
                            }
                        }

                return _isSupported.Value;
            }
        }
    }
}
