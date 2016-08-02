// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.GZipStream
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip
{
    using System;
    using System.IO;
    using System.Text;

    using InternalImplementation;

    public class GZipStream : Stream
    {
        internal static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly Encoding iso8859dash1 = new Iso8859Dash1Encoding();
        internal ZlibBaseStream _baseStream;
        private string _Comment;
        private bool _disposed;
        private string _FileName;
        private bool _firstReadDone;
        private int _headerByteCount;
        public DateTime? LastModified;

        public GZipStream(Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.Default, false)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, false)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen)
            : this(stream, mode, CompressionLevel.Default, leaveOpen)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.GZIP, leaveOpen);
        }

        public string Comment
        {
            get { return _Comment; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                _Comment = value;
            }
        }

        public string FileName
        {
            get { return _FileName; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                _FileName = value;
                if (_FileName == null)
                    return;
                if (_FileName.IndexOf("/") != -1)
                    _FileName = _FileName.Replace("/", "\\");
                if (_FileName.EndsWith("\\"))
                    throw new Exception("Illegal filename");
                if (_FileName.IndexOf("\\") == -1)
                    return;
                _FileName = Path.GetFileName(_FileName);
            }
        }

        public int Crc32 { get; private set; }

        public virtual FlushType FlushMode
        {
            get { return _baseStream._flushMode; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                _baseStream._flushMode = value;
            }
        }

        public int BufferSize
        {
            get { return _baseStream._bufferSize; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                if (_baseStream._workingBuffer != null)
                    throw new ZlibException("The working buffer is already set.");
                if (value < 1024)
                    throw new ZlibException(string.Format("Don't be silly. {0} bytes?? Use a bigger buffer, at least {1}.", value, 1024));
                _baseStream._bufferSize = value;
            }
        }

        public virtual long TotalIn
        {
            get { return _baseStream._z.TotalBytesIn; }
        }

        public virtual long TotalOut
        {
            get { return _baseStream._z.TotalBytesOut; }
        }

        public override bool CanRead
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                return _baseStream._stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("GZipStream");
                return _baseStream._stream.CanWrite;
            }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                    return _baseStream._z.TotalBytesOut + _headerByteCount;
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Reader)
                    return _baseStream._z.TotalBytesIn + _baseStream._gzipHeaderByteCount;
                return 0L;
            }
            set { throw new NotImplementedException(); }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                    return;
                if (disposing && _baseStream != null)
                {
                    _baseStream.Close();
                    Crc32 = _baseStream.Crc32;
                }
                _disposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            if (_disposed)
                throw new ObjectDisposedException("GZipStream");
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("GZipStream");
            var num = _baseStream.Read(buffer, offset, count);
            if (!_firstReadDone)
            {
                _firstReadDone = true;
                FileName = _baseStream._GzipFileName;
                Comment = _baseStream._GzipComment;
            }
            return num;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("GZipStream");
            if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Undefined)
            {
                if (!_baseStream._wantCompress)
                    throw new InvalidOperationException();
                _headerByteCount = EmitHeader();
            }
            _baseStream.Write(buffer, offset, count);
        }

        private int EmitHeader()
        {
            var numArray1 = Comment == null ? null : iso8859dash1.GetBytes(Comment);
            var numArray2 = FileName == null ? null : iso8859dash1.GetBytes(FileName);
            var num1 = Comment == null ? 0 : numArray1.Length + 1;
            var num2 = FileName == null ? 0 : numArray2.Length + 1;
            var buffer = new byte[10 + num1 + num2];
            var num3 = 0;
            var numArray3 = buffer;
            var index1 = num3;
            var num4 = 1;
            var num5 = index1 + num4;
            var num6 = 31;
            numArray3[index1] = (byte) num6;
            var numArray4 = buffer;
            var index2 = num5;
            var num7 = 1;
            var num8 = index2 + num7;
            var num9 = 139;
            numArray4[index2] = (byte) num9;
            var numArray5 = buffer;
            var index3 = num8;
            var num10 = 1;
            var num11 = index3 + num10;
            var num12 = 8;
            numArray5[index3] = (byte) num12;
            byte num13 = 0;
            if (Comment != null)
                num13 ^= 16;
            if (FileName != null)
                num13 ^= 8;
            var numArray6 = buffer;
            var index4 = num11;
            var num14 = 1;
            var destinationIndex1 = index4 + num14;
            int num15 = num13;
            numArray6[index4] = (byte) num15;
            if (!LastModified.HasValue)
                LastModified = DateTime.Now;
            Array.Copy(BitConverter.GetBytes((int) (LastModified.Value - _unixEpoch).TotalSeconds), 0, buffer, destinationIndex1, 4);
            var num16 = destinationIndex1 + 4;
            var numArray7 = buffer;
            var index5 = num16;
            var num17 = 1;
            var num18 = index5 + num17;
            var num19 = 0;
            numArray7[index5] = (byte) num19;
            var numArray8 = buffer;
            var index6 = num18;
            var num20 = 1;
            var destinationIndex2 = index6 + num20;
            int num21 = byte.MaxValue;
            numArray8[index6] = (byte) num21;
            if (num2 != 0)
            {
                Array.Copy(numArray2, 0, buffer, destinationIndex2, num2 - 1);
                var num22 = destinationIndex2 + (num2 - 1);
                var numArray9 = buffer;
                var index7 = num22;
                var num23 = 1;
                destinationIndex2 = index7 + num23;
                var num24 = 0;
                numArray9[index7] = (byte) num24;
            }
            if (num1 != 0)
            {
                Array.Copy(numArray1, 0, buffer, destinationIndex2, num1 - 1);
                var num22 = destinationIndex2 + (num1 - 1);
                var numArray9 = buffer;
                var index7 = num22;
                var num23 = 1;
                var num24 = index7 + num23;
                var num25 = 0;
                numArray9[index7] = (byte) num25;
            }
            _baseStream._stream.Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }

        public static byte[] CompressString(string s)
        {
            using (var memoryStream = new MemoryStream())
            {
                Stream compressor = new GZipStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressString(s, compressor);
                return memoryStream.ToArray();
            }
        }

        public static byte[] CompressBuffer(byte[] b)
        {
            using (var memoryStream = new MemoryStream())
            {
                Stream compressor = new GZipStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressBuffer(b, compressor);
                return memoryStream.ToArray();
            }
        }

        public static string UncompressString(byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                Stream decompressor = new GZipStream(memoryStream, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressString(compressed, decompressor);
            }
        }

        public static byte[] UncompressBuffer(byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                Stream decompressor = new GZipStream(memoryStream, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressBuffer(compressed, decompressor);
            }
        }
    }
}