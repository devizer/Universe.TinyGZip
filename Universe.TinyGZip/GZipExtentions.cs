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

        // case
        // Windows: use builtin compression
        // Linux:   use .gz additional extention
        public static Stream CreateCompressedFile(string fullPath)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                File.WriteAllBytes(fullPath, new byte[0]);
                ProcessStartInfo si = new ProcessStartInfo("compact", "/c \"" + fullPath + "\"");
                si.UseShellExecute = false;
                si.CreateNoWindow = true;
                try
                {
                    using (var p = Process.Start(si))
                    {
                        p.WaitForExit();
                    }

                    FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 16384);
                    return fs;
                }
                catch (Exception)
                {
                    // What the hell?! NON-NTFS on windows????
                }
            }

            FileStream fileStream = new FileStream(fullPath + ".gz", FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 16384);
            Stream gz = CreateCompressor(fileStream);
            return gz;

        }


        public static Stream CreateDecompressor(Stream gzipped)
        {
            return CreateDecompressor(gzipped, false);
        }

        public static Stream CreateDecompressor(Stream gzipped, bool leaveOpen)
        {
            if (gzipped == null)
                throw new ArgumentNullException("gzipped");

            if (IsSystemGZipSupported)
                return new SysGZip.GZipStream(gzipped, SysGZip.CompressionMode.Decompress, leaveOpen);
            else
                return new Universe.TinyGZip.GZipStream(gzipped, CompressionMode.Decompress, leaveOpen);
        }

        public static Stream CreateCompressor(Stream plain, bool leaveOpen)
        {
            if (plain == null)
                throw new ArgumentNullException("plain");

            if (IsSystemGZipSupported)
                return new SysGZip.GZipStream(plain, SysGZip.CompressionMode.Compress, leaveOpen);
            else
                return new Universe.TinyGZip.GZipStream(plain, CompressionMode.Compress, CompressionLevel.Level1, leaveOpen);
        }

        public static Stream CreateCompressor(Stream plain)
        {
            return CreateCompressor(plain, false);
        }

        public static bool IsSystemGZipSupported
        {
            get
            {
                if (!_isSupported.HasValue)
                    lock(Sync)
                        if (!_isSupported.HasValue)
                            _isSupported = IsSystemGZipSupported_Impl();

                return _isSupported.Value;
            }
        }

        static bool IsSystemGZipSupported_Impl()
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

                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(_notSupportedMessage + " We are using TinyGZip implementation" + Environment.NewLine + ex);
                return false;
            }
        }
    }
}
