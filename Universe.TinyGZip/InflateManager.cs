namespace Universe.TinyGZip.InternalImplementation
{
    internal sealed class InflateManager
    {
        private const int PRESET_DICT = 32;
        private const int Z_DEFLATED = 8;

        private static readonly byte[] mark = new byte[4]
        {
            0,
            0,
            byte.MaxValue,
            byte.MaxValue
        };

        internal ZlibCodec _codec;
        private bool _handleRfc1950HeaderBytes = true;
        internal InflateBlocks blocks;
        internal uint computedCheck;
        internal uint expectedCheck;
        internal int marker;
        internal int method;
        private InflateManagerMode mode;
        internal int wbits;

        public InflateManager()
        {
        }

        public InflateManager(bool expectRfc1950HeaderBytes)
        {
            _handleRfc1950HeaderBytes = expectRfc1950HeaderBytes;
        }

        internal bool HandleRfc1950HeaderBytes
        {
            get { return _handleRfc1950HeaderBytes; }
            set { _handleRfc1950HeaderBytes = value; }
        }

        internal int Reset()
        {
            _codec.TotalBytesIn = _codec.TotalBytesOut = 0L;
            _codec.Message = null;
            mode = HandleRfc1950HeaderBytes ? InflateManagerMode.METHOD : InflateManagerMode.BLOCKS;
            var num = (int) blocks.Reset();
            return 0;
        }

        internal int End()
        {
            if (blocks != null)
                blocks.Free();
            blocks = null;
            return 0;
        }

        internal int Initialize(ZlibCodec codec, int w)
        {
            _codec = codec;
            _codec.Message = null;
            blocks = null;
            if (w < 8 || w > 15)
            {
                End();
                throw new ZlibException("Bad window size.");
            }
            wbits = w;
            blocks = new InflateBlocks(codec, HandleRfc1950HeaderBytes ? this : null, 1 << w);
            Reset();
            return 0;
        }

        internal int Inflate(FlushType flush)
        {
            if (_codec.InputBuffer == null)
                throw new ZlibException("InputBuffer is null. ");
            var num1 = 0;
            var r = -5;
            while (true)
            {
                switch (mode)
                {
                    case InflateManagerMode.METHOD:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            if (((method = _codec.InputBuffer[_codec.NextIn++]) & 15) != 8)
                            {
                                mode = InflateManagerMode.BAD;
                                _codec.Message = string.Format("unknown compression method (0x{0:X2})", method);
                                marker = 5;
                                break;
                            }
                            if ((method >> 4) + 8 > wbits)
                            {
                                mode = InflateManagerMode.BAD;
                                _codec.Message = string.Format("invalid window size ({0})", (method >> 4) + 8);
                                marker = 5;
                                break;
                            }
                            mode = InflateManagerMode.FLAG;
                            break;
                        }
                        goto label_4;
                    case InflateManagerMode.FLAG:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            var num2 = _codec.InputBuffer[_codec.NextIn++] & byte.MaxValue;
                            if (((method << 8) + num2)%31 != 0)
                            {
                                mode = InflateManagerMode.BAD;
                                _codec.Message = "incorrect header check";
                                marker = 5;
                                break;
                            }
                            mode = (num2 & 32) == 0 ? InflateManagerMode.BLOCKS : InflateManagerMode.DICT4;
                            break;
                        }
                        goto label_11;
                    case InflateManagerMode.DICT4:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck = (uint) ((ulong) (_codec.InputBuffer[_codec.NextIn++] << 24) & 4278190080UL);
                            mode = InflateManagerMode.DICT3;
                            break;
                        }
                        goto label_16;
                    case InflateManagerMode.DICT3:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck += (uint) (_codec.InputBuffer[_codec.NextIn++] << 16 & 16711680);
                            mode = InflateManagerMode.DICT2;
                            break;
                        }
                        goto label_19;
                    case InflateManagerMode.DICT2:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck += (uint) (_codec.InputBuffer[_codec.NextIn++] << 8 & 65280);
                            mode = InflateManagerMode.DICT1;
                            break;
                        }
                        goto label_22;
                    case InflateManagerMode.DICT1:
                        goto label_24;
                    case InflateManagerMode.DICT0:
                        goto label_27;
                    case InflateManagerMode.BLOCKS:
                        r = blocks.Process(r);
                        if (r == -3)
                        {
                            mode = InflateManagerMode.BAD;
                            marker = 0;
                            break;
                        }
                        if (r == 0)
                            r = num1;
                        if (r == 1)
                        {
                            r = num1;
                            computedCheck = blocks.Reset();
                            if (HandleRfc1950HeaderBytes)
                            {
                                mode = InflateManagerMode.CHECK4;
                                break;
                            }
                            goto label_35;
                        }
                        goto label_33;
                    case InflateManagerMode.CHECK4:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck = (uint) ((ulong) (_codec.InputBuffer[_codec.NextIn++] << 24) & 4278190080UL);
                            mode = InflateManagerMode.CHECK3;
                            break;
                        }
                        goto label_38;
                    case InflateManagerMode.CHECK3:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck += (uint) (_codec.InputBuffer[_codec.NextIn++] << 16 & 16711680);
                            mode = InflateManagerMode.CHECK2;
                            break;
                        }
                        goto label_41;
                    case InflateManagerMode.CHECK2:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck += (uint) (_codec.InputBuffer[_codec.NextIn++] << 8 & 65280);
                            mode = InflateManagerMode.CHECK1;
                            break;
                        }
                        goto label_44;
                    case InflateManagerMode.CHECK1:
                        if (_codec.AvailableBytesIn != 0)
                        {
                            r = num1;
                            --_codec.AvailableBytesIn;
                            ++_codec.TotalBytesIn;
                            expectedCheck += _codec.InputBuffer[_codec.NextIn++] & (uint) byte.MaxValue;
                            if ((int) computedCheck != (int) expectedCheck)
                            {
                                mode = InflateManagerMode.BAD;
                                _codec.Message = "incorrect data check";
                                marker = 5;
                                break;
                            }
                            goto label_50;
                        }
                        goto label_47;
                    case InflateManagerMode.DONE:
                        goto label_51;
                    case InflateManagerMode.BAD:
                        goto label_52;
                    default:
                        goto label_53;
                }
            }
            label_4:
            return r;
            label_11:
            return r;
            label_16:
            return r;
            label_19:
            return r;
            label_22:
            return r;
            label_24:
            if (_codec.AvailableBytesIn == 0)
                return r;
            --_codec.AvailableBytesIn;
            ++_codec.TotalBytesIn;
            expectedCheck += _codec.InputBuffer[_codec.NextIn++] & (uint) byte.MaxValue;
            _codec._Adler32 = expectedCheck;
            mode = InflateManagerMode.DICT0;
            return 2;
            label_27:
            mode = InflateManagerMode.BAD;
            _codec.Message = "need dictionary";
            marker = 0;
            return -2;
            label_33:
            return r;
            label_35:
            mode = InflateManagerMode.DONE;
            return 1;
            label_38:
            return r;
            label_41:
            return r;
            label_44:
            return r;
            label_47:
            return r;
            label_50:
            mode = InflateManagerMode.DONE;
            return 1;
            label_51:
            return 1;
            label_52:
            throw new ZlibException(string.Format("Bad state ({0})", _codec.Message));
            label_53:
            throw new ZlibException("Stream error.");
        }

        internal int SetDictionary(byte[] dictionary)
        {
            var start = 0;
            var n = dictionary.Length;
            if (mode != InflateManagerMode.DICT0)
                throw new ZlibException("Stream error.");
            if ((int) Adler.Adler32(1U, dictionary, 0, dictionary.Length) != (int) _codec._Adler32)
                return -3;
            _codec._Adler32 = Adler.Adler32(0U, null, 0, 0);
            if (n >= 1 << wbits)
            {
                n = (1 << wbits) - 1;
                start = dictionary.Length - n;
            }
            blocks.SetDictionary(dictionary, start, n);
            mode = InflateManagerMode.BLOCKS;
            return 0;
        }

        internal int Sync()
        {
            if (mode != InflateManagerMode.BAD)
            {
                mode = InflateManagerMode.BAD;
                marker = 0;
            }
            int num1;
            if ((num1 = _codec.AvailableBytesIn) == 0)
                return -5;
            var index1 = _codec.NextIn;
            int index2;
            for (index2 = marker; num1 != 0 && index2 < 4; --num1)
            {
                if (_codec.InputBuffer[index1] == mark[index2])
                    ++index2;
                else
                    index2 = (int) _codec.InputBuffer[index1] == 0 ? 4 - index2 : 0;
                ++index1;
            }
            _codec.TotalBytesIn += index1 - _codec.NextIn;
            _codec.NextIn = index1;
            _codec.AvailableBytesIn = num1;
            marker = index2;
            if (index2 != 4)
                return -3;
            var num2 = _codec.TotalBytesIn;
            var num3 = _codec.TotalBytesOut;
            Reset();
            _codec.TotalBytesIn = num2;
            _codec.TotalBytesOut = num3;
            mode = InflateManagerMode.BLOCKS;
            return 0;
        }

        internal int SyncPoint(ZlibCodec z)
        {
            return blocks.SyncPoint();
        }

        private enum InflateManagerMode
        {
            METHOD,
            FLAG,
            DICT4,
            DICT3,
            DICT2,
            DICT1,
            DICT0,
            BLOCKS,
            CHECK4,
            CHECK3,
            CHECK2,
            CHECK1,
            DONE,
            BAD
        }
    }
}