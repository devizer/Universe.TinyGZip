// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.ZlibBaseStream
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    #pragma warning disable 642, 219
    internal class ZlibBaseStream : Stream
    {
        protected internal byte[] _buf1 = new byte[1];
        protected internal int _bufferSize = 16384;
        protected internal CompressionMode _compressionMode;
        protected internal ZlibStreamFlavor _flavor;
        protected internal FlushType _flushMode;
        protected internal string _GzipComment;
        protected internal string _GzipFileName;
        protected internal int _gzipHeaderByteCount;
        protected internal DateTime _GzipMtime;
        protected internal bool _leaveOpen;
        protected internal CompressionLevel _level;
        protected internal Stream _stream;
        protected internal StreamMode _streamMode = StreamMode.Undefined;
        protected internal byte[] _workingBuffer;
        protected internal ZlibCodec _z;
        private readonly CRC32 crc;
        private bool nomoreinput;
        protected internal CompressionStrategy Strategy = CompressionStrategy.Default;

        public ZlibBaseStream(Stream stream, CompressionMode compressionMode, CompressionLevel level, ZlibStreamFlavor flavor, bool leaveOpen)
        {
            _flushMode = FlushType.None;
            _stream = stream;
            _leaveOpen = leaveOpen;
            _compressionMode = compressionMode;
            _flavor = flavor;
            _level = level;
            if (flavor != ZlibStreamFlavor.GZIP)
                return;
            crc = new CRC32();
        }

        internal int Crc32
        {
            get
            {
                if (crc == null)
                    return 0;
                return crc.Crc32Result;
            }
        }

        protected internal bool _wantCompress
        {
            get { return _compressionMode == CompressionMode.Compress; }
        }

        private ZlibCodec z
        {
            get
            {
                if (_z == null)
                {
                    var flag = _flavor == ZlibStreamFlavor.ZLIB;
                    _z = new ZlibCodec();
                    if (_compressionMode == CompressionMode.Decompress)
                    {
                        _z.InitializeInflate(flag);
                    }
                    else
                    {
                        _z.Strategy = Strategy;
                        _z.InitializeDeflate(_level, flag);
                    }
                }
                return _z;
            }
        }

        private byte[] workingBuffer
        {
            get
            {
                if (_workingBuffer == null)
                    _workingBuffer = new byte[_bufferSize];
                return _workingBuffer;
            }
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (crc != null)
                crc.SlurpBlock(buffer, offset, count);
            if (_streamMode == StreamMode.Undefined)
                _streamMode = StreamMode.Writer;
            else if (_streamMode != StreamMode.Writer)
                throw new ZlibException("Cannot Write after Reading.");
            if (count == 0)
                return;
            z.InputBuffer = buffer;
            _z.NextIn = offset;
            _z.AvailableBytesIn = count;
            bool flag;
            do
            {
                _z.OutputBuffer = workingBuffer;
                _z.NextOut = 0;
                _z.AvailableBytesOut = _workingBuffer.Length;
                var num = _wantCompress ? _z.Deflate(_flushMode) : _z.Inflate(_flushMode);
                if (num != 0 && num != 1)
                    throw new ZlibException((_wantCompress ? "de" : "in") + "flating: " + _z.Message);
                _stream.Write(_workingBuffer, 0, _workingBuffer.Length - _z.AvailableBytesOut);
                flag = _z.AvailableBytesIn == 0 && _z.AvailableBytesOut != 0;
                if (_flavor == ZlibStreamFlavor.GZIP && !_wantCompress)
                    flag = _z.AvailableBytesIn == 8 && _z.AvailableBytesOut != 0;
            } while (!flag);
        }

        private void finish()
        {
            if (_z == null)
                return;
            if (_streamMode == StreamMode.Writer)
            {
                bool flag;
                do
                {
                    _z.OutputBuffer = workingBuffer;
                    _z.NextOut = 0;
                    _z.AvailableBytesOut = _workingBuffer.Length;
                    var num = _wantCompress ? _z.Deflate(FlushType.Finish) : _z.Inflate(FlushType.Finish);
                    if (num != 1 && num != 0)
                    {
                        var str = (_wantCompress ? "de" : "in") + "flating";
                        if (_z.Message == null)
                            throw new ZlibException(string.Format("{0}: (rc = {1})", str, num));
                        throw new ZlibException(str + ": " + _z.Message);
                    }
                    if (_workingBuffer.Length - _z.AvailableBytesOut > 0)
                        _stream.Write(_workingBuffer, 0, _workingBuffer.Length - _z.AvailableBytesOut);
                    flag = _z.AvailableBytesIn == 0 && _z.AvailableBytesOut != 0;
                    if (_flavor == ZlibStreamFlavor.GZIP && !_wantCompress)
                        flag = _z.AvailableBytesIn == 8 && _z.AvailableBytesOut != 0;
                } while (!flag);
                Flush();
                if (_flavor != ZlibStreamFlavor.GZIP)
                    return;
                if (!_wantCompress)
                    throw new ZlibException("Writing with decompression is not supported.");
                _stream.Write(BitConverter.GetBytes(crc.Crc32Result), 0, 4);
                _stream.Write(BitConverter.GetBytes((int) (crc.TotalBytesRead & uint.MaxValue)), 0, 4);
            }
            else
            {
                if (_streamMode != StreamMode.Reader || _flavor != ZlibStreamFlavor.GZIP)
                    return;
                if (_wantCompress)
                    throw new ZlibException("Reading with compression is not supported.");
                if (_z.TotalBytesOut == 0L)
                    return;
                var buffer = new byte[8];
                if (_z.AvailableBytesIn < 8)
                {
                    Array.Copy(_z.InputBuffer, _z.NextIn, buffer, 0, _z.AvailableBytesIn);
                    var count = 8 - _z.AvailableBytesIn;
                    var num = _stream.Read(buffer, _z.AvailableBytesIn, count);
                    if (count != num)
                        throw new ZlibException(string.Format("Missing or incomplete GZIP trailer. Expected 8 bytes, got {0}.",
                            _z.AvailableBytesIn + num));
                }
                else
                    Array.Copy(_z.InputBuffer, _z.NextIn, buffer, 0, buffer.Length);
                var num1 = BitConverter.ToInt32(buffer, 0);
                var crc32Result = crc.Crc32Result;
                var num2 = BitConverter.ToInt32(buffer, 4);
                var num3 = (int) (_z.TotalBytesOut & uint.MaxValue);
                if (crc32Result != num1)
                    throw new ZlibException(string.Format("Bad CRC32 in GZIP trailer. (actual({0:X8})!=expected({1:X8}))", crc32Result, num1));
                if (num3 != num2)
                    throw new ZlibException(string.Format("Bad size in GZIP trailer. (actual({0})!=expected({1}))", num3, num2));
            }
        }

        private void end()
        {
            if (z == null)
                return;
            if (_wantCompress)
                _z.EndDeflate();
            else
                _z.EndInflate();
            _z = null;
        }

        public override void Close()
        {
            if (_stream == null)
                return;
            try
            {
                finish();
            }
            finally
            {
                end();
                if (!_leaveOpen)
                    _stream.Close();
                _stream = null;
            }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        private string ReadZeroTerminatedString()
        {
            var list = new List<byte>();
            var flag = false;
            while (_stream.Read(_buf1, 0, 1) == 1)
            {
                if (_buf1[0] == 0)
                    flag = true;
                else
                    list.Add(_buf1[0]);
                if (flag)
                {
                    var bytes = list.ToArray();
                    return GZipStream.iso8859dash1.GetString(bytes, 0, bytes.Length);
                }
            }
            throw new ZlibException("Unexpected EOF reading GZIP header.");
        }

        private int _ReadAndValidateGzipHeader()
        {
            var num1 = 0;
            var buffer1 = new byte[10];
            var num2 = _stream.Read(buffer1, 0, buffer1.Length);
            switch (num2)
            {
                case 0:
                    return 0;
                case 10:
                    if (buffer1[0] != 31 || buffer1[1] != 139 || buffer1[2] != 8)
                        throw new ZlibException("Bad GZIP header.");
                    var num3 = BitConverter.ToInt32(buffer1, 4);
                    _GzipMtime = GZipStream._unixEpoch.AddSeconds(num3);
                    var num4 = num1 + num2;
                    if ((buffer1[3] & 4) == 4)
                    {
                        var num5 = _stream.Read(buffer1, 0, 2);
                        var num6 = num4 + num5;
                        var num7 = (short) (buffer1[0] + buffer1[1]*256);
                        var buffer2 = new byte[num7];
                        var num8 = _stream.Read(buffer2, 0, buffer2.Length);
                        if (num8 != num7)
                            throw new ZlibException("Unexpected end-of-file reading GZIP header.");
                        num4 = num6 + num8;
                    }
                    if ((buffer1[3] & 8) == 8)
                        _GzipFileName = ReadZeroTerminatedString();
                    if ((buffer1[3] & 16) == 16)
                        _GzipComment = ReadZeroTerminatedString();
                    if ((buffer1[3] & 2) == 2)
                        Read(_buf1, 0, 1);
                    return num4;
                default:
                    throw new ZlibException("Not a valid GZIP stream.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_streamMode == StreamMode.Undefined)
            {
                if (!_stream.CanRead)
                    throw new ZlibException("The stream is not readable.");
                _streamMode = StreamMode.Reader;
                z.AvailableBytesIn = 0;
                if (_flavor == ZlibStreamFlavor.GZIP)
                {
                    _gzipHeaderByteCount = _ReadAndValidateGzipHeader();
                    if (_gzipHeaderByteCount == 0)
                        return 0;
                }
            }
            if (_streamMode != StreamMode.Reader)
                throw new ZlibException("Cannot Read after Writing.");
            if (count == 0 || nomoreinput && _wantCompress)
                return 0;
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (offset < buffer.GetLowerBound(0))
                throw new ArgumentOutOfRangeException("offset");
            if (offset + count > buffer.GetLength(0))
                throw new ArgumentOutOfRangeException("count");
            _z.OutputBuffer = buffer;
            _z.NextOut = offset;
            _z.AvailableBytesOut = count;
            _z.InputBuffer = workingBuffer;
            int num1;
            do
            {
                if (_z.AvailableBytesIn == 0 && !nomoreinput)
                {
                    _z.NextIn = 0;
                    _z.AvailableBytesIn = _stream.Read(_workingBuffer, 0, _workingBuffer.Length);
                    if (_z.AvailableBytesIn == 0)
                        nomoreinput = true;
                }
                num1 = _wantCompress ? _z.Deflate(_flushMode) : _z.Inflate(_flushMode);
                if (nomoreinput && num1 == -5)
                    return 0;
                if (num1 != 0 && num1 != 1)
                    throw new ZlibException(string.Format("{0}flating:  rc={1}  msg={2}", _wantCompress ? "de" : "in", num1, _z.Message));
            } while ((!nomoreinput && num1 != 1 || _z.AvailableBytesOut != count) && (_z.AvailableBytesOut > 0 && !nomoreinput && num1 == 0));
            if (_z.AvailableBytesOut > 0)
            {
                if (num1 != 0 || _z.AvailableBytesIn != 0)
                    ;
                if (nomoreinput && _wantCompress)
                {
                    var num2 = _z.Deflate(FlushType.Finish);
                    if (num2 != 0 && num2 != 1)
                        throw new ZlibException(string.Format("Deflating:  rc={0}  msg={1}", num2, _z.Message));
                }
            }
            var count1 = count - _z.AvailableBytesOut;
            if (crc != null)
                crc.SlurpBlock(buffer, offset, count1);
            return count1;
        }

        public static void CompressString(string s, Stream compressor)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            using (compressor)
                compressor.Write(bytes, 0, bytes.Length);
        }

        public static void CompressBuffer(byte[] b, Stream compressor)
        {
            using (compressor)
                compressor.Write(b, 0, b.Length);
        }

        public static string UncompressString(byte[] compressed, Stream decompressor)
        {
            var buffer = new byte[1024];
            var utF8 = Encoding.UTF8;
            using (var memoryStream = new MemoryStream())
            {
                using (decompressor)
                {
                    int count;
                    while ((count = decompressor.Read(buffer, 0, buffer.Length)) != 0)
                        memoryStream.Write(buffer, 0, count);
                }
                memoryStream.Seek(0L, SeekOrigin.Begin);
                return new StreamReader(memoryStream, utF8).ReadToEnd();
            }
        }

        public static byte[] UncompressBuffer(byte[] compressed, Stream decompressor)
        {
            var buffer = new byte[1024];
            using (var memoryStream = new MemoryStream())
            {
                using (decompressor)
                {
                    int count;
                    while ((count = decompressor.Read(buffer, 0, buffer.Length)) != 0)
                        memoryStream.Write(buffer, 0, count);
                }
                return memoryStream.ToArray();
            }
        }

        internal enum StreamMode
        {
            Writer,
            Reader,
            Undefined
        }
    }
}