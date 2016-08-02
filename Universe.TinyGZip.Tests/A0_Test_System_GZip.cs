namespace H3Control.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using NUnit.Framework;

    using Universe;
    using Universe.TinyGZip;

    using CompressionMode = System.IO.Compression.CompressionMode;
    using GZipStream = System.IO.Compression.GZipStream;

    [TestFixture]
    public class A0_Test_System_GZip 
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
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
    }
}