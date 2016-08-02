// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.ParallelDeflateOutputStream
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using InternalImplementation;

    public class ParallelDeflateOutputStream : Stream
    {
        private static readonly int IO_BUFFER_SIZE_DEFAULT = 65536;
        private static readonly int BufferPairsPerCore = 4;
        private int _bufferSize = IO_BUFFER_SIZE_DEFAULT;
        private readonly CompressionLevel _compressLevel;
        private int _currentlyFilling;

        private readonly TraceBits _DesiredTrace = TraceBits.EmitAll | TraceBits.EmitEnter | TraceBits.Session | TraceBits.Compress |
                                                   TraceBits.WriteEnter | TraceBits.WriteTake;

        private readonly object _eLock = new object();
        private bool _firstWriteDone;
        private bool _handlingException;
        private bool _isClosed;
        private int _lastFilled;
        private int _lastWritten;
        private int _latestCompressed;
        private readonly object _latestLock = new object();
        private readonly bool _leaveOpen;
        private int _maxBufferPairs;
        private AutoResetEvent _newlyCompressedBlob;
        private readonly object _outputLock = new object();
        private Stream _outStream;
        private volatile Exception _pendingException;
        private List<WorkItem> _pool;
        private CRC32 _runningCrc;
        private Queue<int> _toFill;
        private Queue<int> _toWrite;
        private bool emitting;

        public ParallelDeflateOutputStream(Stream stream)
            : this(stream, CompressionLevel.Default, CompressionStrategy.Default, false)
        {
        }

        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level)
            : this(stream, level, CompressionStrategy.Default, false)
        {
        }

        public ParallelDeflateOutputStream(Stream stream, bool leaveOpen)
            : this(stream, CompressionLevel.Default, CompressionStrategy.Default, leaveOpen)
        {
        }

        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level, bool leaveOpen)
            : this(stream, CompressionLevel.Default, CompressionStrategy.Default, leaveOpen)
        {
        }

        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level, CompressionStrategy strategy, bool leaveOpen)
        {
            _outStream = stream;
            _compressLevel = level;
            Strategy = strategy;
            _leaveOpen = leaveOpen;
            MaxBufferPairs = 16;
        }

        public CompressionStrategy Strategy { get; private set; }

        public int MaxBufferPairs
        {
            get { return _maxBufferPairs; }
            set
            {
                if (value < 4)
                    throw new ArgumentException("MaxBufferPairs", "Value must be 4 or greater.");
                _maxBufferPairs = value;
            }
        }

        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                if (value < 1024)
                    throw new ArgumentOutOfRangeException("BufferSize", "BufferSize must be greater than 1024 bytes");
                _bufferSize = value;
            }
        }

        public int Crc32 { get; private set; }

        public long BytesProcessed { get; private set; }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _outStream.CanWrite; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { return _outStream.Position; }
            set { throw new NotSupportedException(); }
        }

        private void _InitializePoolOfWorkItems()
        {
            _toWrite = new Queue<int>();
            _toFill = new Queue<int>();
            _pool = new List<WorkItem>();
            var num = Math.Min(BufferPairsPerCore*Environment.ProcessorCount, _maxBufferPairs);
            for (var ix = 0; ix < num; ++ix)
            {
                _pool.Add(new WorkItem(_bufferSize, _compressLevel, Strategy, ix));
                _toFill.Enqueue(ix);
            }
            _newlyCompressedBlob = new AutoResetEvent(false);
            _runningCrc = new CRC32();
            _currentlyFilling = -1;
            _lastFilled = -1;
            _lastWritten = -1;
            _latestCompressed = -1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var mustWait = false;
            if (_isClosed)
                throw new InvalidOperationException();
            if (_pendingException != null)
            {
                _handlingException = true;
                var exception = _pendingException;
                _pendingException = null;
                throw exception;
            }
            if (count == 0)
                return;
            if (!_firstWriteDone)
            {
                _InitializePoolOfWorkItems();
                _firstWriteDone = true;
            }
            do
            {
                EmitPendingBuffers(false, mustWait);
                mustWait = false;
                int index;
                if (_currentlyFilling >= 0)
                    index = _currentlyFilling;
                else if (_toFill.Count == 0)
                {
                    mustWait = true;
                    goto label_20;
                }
                else
                {
                    index = _toFill.Dequeue();
                    ++_lastFilled;
                }
                var workItem = _pool[index];
                var count1 = workItem.buffer.Length - workItem.inputBytesAvailable > count
                    ? count
                    : workItem.buffer.Length - workItem.inputBytesAvailable;
                workItem.ordinal = _lastFilled;
                Buffer.BlockCopy(buffer, offset, workItem.buffer, workItem.inputBytesAvailable, count1);
                count -= count1;
                offset += count1;
                workItem.inputBytesAvailable += count1;
                if (workItem.inputBytesAvailable == workItem.buffer.Length)
                {
                    if (!ThreadPool.QueueUserWorkItem(_DeflateOne, workItem))
                        throw new Exception("Cannot enqueue workitem");
                    _currentlyFilling = -1;
                }
                else
                    _currentlyFilling = index;
                if (count <= 0)
                    ;
                label_20:
                ;
            } while (count > 0);
        }

        private void _FlushFinish()
        {
            var buffer = new byte[128];
            var zlibCodec = new ZlibCodec();
            zlibCodec.InitializeDeflate(_compressLevel, false);
            zlibCodec.InputBuffer = null;
            zlibCodec.NextIn = 0;
            zlibCodec.AvailableBytesIn = 0;
            zlibCodec.OutputBuffer = buffer;
            zlibCodec.NextOut = 0;
            zlibCodec.AvailableBytesOut = buffer.Length;
            var num = zlibCodec.Deflate(FlushType.Finish);
            if (num != 1 && num != 0)
                throw new Exception("deflating: " + zlibCodec.Message);
            if (buffer.Length - zlibCodec.AvailableBytesOut > 0)
                _outStream.Write(buffer, 0, buffer.Length - zlibCodec.AvailableBytesOut);
            zlibCodec.EndDeflate();
            Crc32 = _runningCrc.Crc32Result;
        }

        private void _Flush(bool lastInput)
        {
            if (_isClosed)
                throw new InvalidOperationException();
            if (emitting)
                return;
            if (_currentlyFilling >= 0)
            {
                _DeflateOne(_pool[_currentlyFilling]);
                _currentlyFilling = -1;
            }
            if (lastInput)
            {
                EmitPendingBuffers(true, false);
                _FlushFinish();
            }
            else
                EmitPendingBuffers(false, false);
        }

        public override void Flush()
        {
            if (_pendingException != null)
            {
                _handlingException = true;
                var exception = _pendingException;
                _pendingException = null;
                throw exception;
            }
            if (_handlingException)
                return;
            _Flush(false);
        }

        public override void Close()
        {
            if (_pendingException != null)
            {
                _handlingException = true;
                var exception = _pendingException;
                _pendingException = null;
                throw exception;
            }
            if (_handlingException || _isClosed)
                return;
            _Flush(true);
            if (!_leaveOpen)
                _outStream.Close();
            _isClosed = true;
        }

        public new void Dispose()
        {
            Close();
            _pool = null;
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public void Reset(Stream stream)
        {
            if (!_firstWriteDone)
                return;
            _toWrite.Clear();
            _toFill.Clear();
            foreach (var workItem in _pool)
            {
                _toFill.Enqueue(workItem.index);
                workItem.ordinal = -1;
            }
            _firstWriteDone = false;
            BytesProcessed = 0L;
            _runningCrc = new CRC32();
            _isClosed = false;
            _currentlyFilling = -1;
            _lastFilled = -1;
            _lastWritten = -1;
            _latestCompressed = -1;
            _outStream = stream;
        }

        private void EmitPendingBuffers(bool doAll, bool mustWait)
        {
            if (emitting)
                return;
            emitting = true;
            if (doAll || mustWait)
                _newlyCompressedBlob.WaitOne();
            do
            {
                var num = -1;
                var millisecondsTimeout = doAll ? 200 : (mustWait ? -1 : 0);
                int index;
                do
                {
                    if (Monitor.TryEnter(_toWrite, millisecondsTimeout))
                    {
                        index = -1;
                        try
                        {
                            if (_toWrite.Count > 0)
                                index = _toWrite.Dequeue();
                        }
                        finally
                        {
                            Monitor.Exit(_toWrite);
                        }
                        if (index >= 0)
                        {
                            var workItem = _pool[index];
                            if (workItem.ordinal != _lastWritten + 1)
                            {
                                lock (_toWrite)
                                    _toWrite.Enqueue(index);
                                if (num == index)
                                {
                                    _newlyCompressedBlob.WaitOne();
                                    num = -1;
                                }
                                else if (num == -1)
                                {
                                    num = index;
                                }
                                else
                                    goto label_24;
                            }
                            else
                            {
                                num = -1;
                                _outStream.Write(workItem.compressed, 0, workItem.compressedBytesAvailable);
                                _runningCrc.Combine(workItem.crc, workItem.inputBytesAvailable);
                                BytesProcessed += workItem.inputBytesAvailable;
                                workItem.inputBytesAvailable = 0;
                                _lastWritten = workItem.ordinal;
                                _toFill.Enqueue(workItem.index);
                                if (millisecondsTimeout == -1)
                                    millisecondsTimeout = 0;
                            }
                        }
                    }
                    else
                        index = -1;
                    label_24:
                    ;
                } while (index >= 0);
            } while (doAll && _lastWritten != _latestCompressed);
            emitting = false;
        }

        private void _DeflateOne(object wi)
        {
            var workitem = (WorkItem) wi;
            try
            {
                var num = workitem.index;
                var crC32 = new CRC32();
                crC32.SlurpBlock(workitem.buffer, 0, workitem.inputBytesAvailable);
                DeflateOneSegment(workitem);
                workitem.crc = crC32.Crc32Result;
                lock (_latestLock)
                {
                    if (workitem.ordinal > _latestCompressed)
                        _latestCompressed = workitem.ordinal;
                }
                lock (_toWrite)
                    _toWrite.Enqueue(workitem.index);
                _newlyCompressedBlob.Set();
            }
            catch (Exception ex)
            {
                lock (_eLock)
                {
                    if (_pendingException == null)
                        return;
                    _pendingException = ex;
                }
            }
        }

        private bool DeflateOneSegment(WorkItem workitem)
        {
            var zlibCodec = workitem.compressor;
            var num = 0;
            zlibCodec.ResetDeflate();
            zlibCodec.NextIn = 0;
            zlibCodec.AvailableBytesIn = workitem.inputBytesAvailable;
            zlibCodec.NextOut = 0;
            zlibCodec.AvailableBytesOut = workitem.compressed.Length;
            do
            {
                zlibCodec.Deflate(FlushType.None);
            } while (zlibCodec.AvailableBytesIn > 0 || zlibCodec.AvailableBytesOut == 0);
            num = zlibCodec.Deflate(FlushType.Sync);
            workitem.compressedBytesAvailable = (int) zlibCodec.TotalBytesOut;
            return true;
        }

        [Conditional("Trace")]
        private void TraceOutput(TraceBits bits, string format, params object[] varParams)
        {
            if ((bits & _DesiredTrace) == TraceBits.None)
                return;
            lock (_outputLock)
            {
                var local_0 = Thread.CurrentThread.GetHashCode();
                Console.ForegroundColor = (ConsoleColor) (local_0%8 + 8);
                Console.Write("{0:000} PDOS ", local_0);
                Console.WriteLine(format, varParams);
                Console.ResetColor();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        [Flags]
        private enum TraceBits : uint
        {
            None = 0U,
            NotUsed1 = 1U,
            EmitLock = 2U,
            EmitEnter = 4U,
            EmitBegin = 8U,
            EmitDone = 16U,
            EmitSkip = 32U,
            EmitAll = EmitSkip | EmitDone | EmitBegin | EmitLock,
            Flush = 64U,
            Lifecycle = 128U,
            Session = 256U,
            Synch = 512U,
            Instance = 1024U,
            Compress = 2048U,
            Write = 4096U,
            WriteEnter = 8192U,
            WriteTake = 16384U,
            All = 4294967295U
        }
    }
}