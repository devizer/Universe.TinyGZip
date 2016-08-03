namespace H3Control.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using NUnit.Framework;

    using Universe;
    using Universe.TinyGZip;

    using CompressionMode = System.IO.Compression.CompressionMode;
    using GZipStream = System.IO.Compression.GZipStream;

    [TestFixture]
    public class A0_Test_System_GZip : BaseTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
        }

        [Test]
        public void T00_Display_Is_System_GZip_Supported()
        {
            Trace.WriteLine("GZipExtentions.IsSystemGZipSupported: " + GZipExtentions.IsSystemGZipSupported);
        }

        [Test]
        public void T01_GZip_Doesnt_Fail()
        {
            try
            {
                byte[] plain = {(byte) 5, 4, 3, 2, 1};
                MemoryStream mem = new MemoryStream();
                using (GZipStream gzip = new GZipStream(mem, CompressionMode.Compress, true))
                {
                    gzip.Write(plain, 0, plain.Length);
                }

                Trace.WriteLine("Compressed {5,4,3,2,1} length is " + mem.Length);
                string asCSharp = string.Join(",", mem.ToArray().Select(x => "0x" + x.ToString("x2")));
                Trace.WriteLine("{" + asCSharp + "}");

                mem.Position = 0;
                MemoryStream copy = new MemoryStream();
                using (GZipStream ungzip = new GZipStream(mem, CompressionMode.Decompress, true))
                {
                    ungzip.CopyTo(copy);
                }

                Assert.AreEqual(Convert.ToBase64String(plain), Convert.ToBase64String(copy.ToArray()));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("GZip streams are not supported on this mono configuration. Please use SharpZipLib and DotNetZip");
                Assert.Fail("System GZip doesnt work" + Environment.NewLine + ex);
            }
        }

        [Test]
        public void T01_System_GZip_of_Zero_Length_Stream()
        {
            byte[] plain = {};
            MemoryStream mem = new MemoryStream();
            using (GZipStream gzip = new GZipStream(mem, CompressionMode.Compress, true))
            {
                gzip.Write(plain, 0, plain.Length);
            }

            Trace.WriteLine("The output of Compressed ZERO-length by System is 0x" + string.Join("", mem.ToArray().Select(x => x.ToString("X2"))));
        }

    }
}