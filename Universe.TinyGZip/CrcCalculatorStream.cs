// Decompiled with JetBrains decompiler
// Type: Ionic.Crc.CrcCalculatorStream
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;
    using System.IO;

    public class CrcCalculatorStream : Stream, IDisposable
    {
        private static readonly long UnsetLengthLimit = -99L;
        private readonly CRC32 _Crc32;
        internal Stream _innerStream;
        private readonly long _lengthLimit = -99L;

        public CrcCalculatorStream(Stream stream)
            : this(true, UnsetLengthLimit, stream, null)
        {
        }

        public CrcCalculatorStream(Stream stream, bool leaveOpen)
            : this(leaveOpen, UnsetLengthLimit, stream, null)
        {
        }

        public CrcCalculatorStream(Stream stream, long length)
            : this(true, length, stream, null)
        {
            if (length < 0L)
                throw new ArgumentException("length");
        }

        public CrcCalculatorStream(Stream stream, long length, bool leaveOpen)
            : this(leaveOpen, length, stream, null)
        {
            if (length < 0L)
                throw new ArgumentException("length");
        }

        public CrcCalculatorStream(Stream stream, long length, bool leaveOpen, CRC32 crc32)
            : this(leaveOpen, length, stream, crc32)
        {
            if (length < 0L)
                throw new ArgumentException("length");
        }

        private CrcCalculatorStream(bool leaveOpen, long length, Stream stream, CRC32 crc32)
        {
            _innerStream = stream;
            _Crc32 = crc32 ?? new CRC32();
            _lengthLimit = length;
            LeaveOpen = leaveOpen;
        }

        public long TotalBytesSlurped
        {
            get { return _Crc32.TotalBytesRead; }
        }

        public int Crc
        {
            get { return _Crc32.Crc32Result; }
        }

        public bool LeaveOpen { get; set; }

        public override bool CanRead
        {
            get { return _innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _innerStream.CanWrite; }
        }

        public override long Length
        {
            get
            {
                if (_lengthLimit == UnsetLengthLimit)
                    return _innerStream.Length;
                return _lengthLimit;
            }
        }

        public override long Position
        {
            get { return _Crc32.TotalBytesRead; }
            set { throw new NotSupportedException(); }
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var count1 = count;
            if (_lengthLimit != UnsetLengthLimit)
            {
                if (_Crc32.TotalBytesRead >= _lengthLimit)
                    return 0;
                var num = _lengthLimit - _Crc32.TotalBytesRead;
                if (num < count)
                    count1 = (int) num;
            }
            var count2 = _innerStream.Read(buffer, offset, count1);
            if (count2 > 0)
                _Crc32.SlurpBlock(buffer, offset, count2);
            return count2;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0)
                _Crc32.SlurpBlock(buffer, offset, count);
            _innerStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            base.Close();
            if (LeaveOpen)
                return;
            _innerStream.Close();
        }
    }
}