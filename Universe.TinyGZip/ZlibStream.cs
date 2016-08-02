﻿// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.ZlibStream
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;
    using System.IO;

    public class ZlibStream : Stream
    {
        internal ZlibBaseStream _baseStream;
        private bool _disposed;

        public ZlibStream(Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.Default, false)
        {
        }

        public ZlibStream(Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, false)
        {
        }

        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen)
            : this(stream, mode, CompressionLevel.Default, leaveOpen)
        {
        }

        public ZlibStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
        }

        public virtual FlushType FlushMode
        {
            get { return _baseStream._flushMode; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("ZlibStream");
                _baseStream._flushMode = value;
            }
        }

        public int BufferSize
        {
            get { return _baseStream._bufferSize; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("ZlibStream");
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
                    throw new ObjectDisposedException("ZlibStream");
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
                    throw new ObjectDisposedException("ZlibStream");
                return _baseStream._stream.CanWrite;
            }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                    return _baseStream._z.TotalBytesOut;
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Reader)
                    return _baseStream._z.TotalBytesIn;
                return 0L;
            }
            set { throw new NotSupportedException(); }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                    return;
                if (disposing && _baseStream != null)
                    _baseStream.Close();
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
                throw new ObjectDisposedException("ZlibStream");
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("ZlibStream");
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("ZlibStream");
            _baseStream.Write(buffer, offset, count);
        }

        public static byte[] CompressString(string s)
        {
            using (var memoryStream = new MemoryStream())
            {
                Stream compressor = new ZlibStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressString(s, compressor);
                return memoryStream.ToArray();
            }
        }

        public static byte[] CompressBuffer(byte[] b)
        {
            using (var memoryStream = new MemoryStream())
            {
                Stream compressor = new ZlibStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressBuffer(b, compressor);
                return memoryStream.ToArray();
            }
        }

        public static string UncompressString(byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                Stream decompressor = new ZlibStream(memoryStream, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressString(compressed, decompressor);
            }
        }

        public static byte[] UncompressBuffer(byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                Stream decompressor = new ZlibStream(memoryStream, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressBuffer(compressed, decompressor);
            }
        }
    }
}