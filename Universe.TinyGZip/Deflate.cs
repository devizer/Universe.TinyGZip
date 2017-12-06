// Decompiled with JetBrains decompiler
// Type: Ionic.Zlib.DeflateManager
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;

    #pragma warning disable 642, 219
    internal sealed class DeflateManager
    {
        private static readonly int MEM_LEVEL_MAX = 9;
        private static readonly int MEM_LEVEL_DEFAULT = 8;

        private static readonly string[] _ErrorMessage = new string[10]
        {
            "need dictionary",
            "stream end",
            "",
            "file error",
            "stream error",
            "data error",
            "insufficient memory",
            "buffer error",
            "incompatible version",
            ""
        };

        private static readonly int PRESET_DICT = 32;
        private static readonly int INIT_STATE = 42;
        private static readonly int BUSY_STATE = 113;
        private static readonly int FINISH_STATE = 666;
        private static readonly int Z_DEFLATED = 8;
        private static readonly int STORED_BLOCK = 0;
        private static readonly int STATIC_TREES = 1;
        private static readonly int DYN_TREES = 2;
        private static readonly int Z_BINARY = 0;
        private static readonly int Z_ASCII = 1;
        private static readonly int Z_UNKNOWN = 2;
        private static readonly int Buf_size = 16;
        private static readonly int MIN_MATCH = 3;
        private static readonly int MAX_MATCH = 258;
        private static readonly int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;
        private static readonly int HEAP_SIZE = 2*InternalConstants.L_CODES + 1;
        private static readonly int END_BLOCK = 256;
        internal ZlibCodec _codec;
        internal int _distanceOffset;
        internal int _lengthOffset;
        private bool _WantRfc1950HeaderBytes = true;
        internal short bi_buf;
        internal int bi_valid;
        internal short[] bl_count = new short[InternalConstants.MAX_BITS + 1];
        internal short[] bl_tree;
        internal int block_start;
        internal CompressionLevel compressionLevel;
        internal CompressionStrategy compressionStrategy;
        private Config config;
        internal sbyte data_type;
        private CompressFunc DeflateFunction;
        internal sbyte[] depth = new sbyte[2*InternalConstants.L_CODES + 1];
        internal short[] dyn_dtree;
        internal short[] dyn_ltree;
        internal int hash_bits;
        internal int hash_mask;
        internal int hash_shift;
        internal int hash_size;
        internal short[] head;
        internal int[] heap = new int[2*InternalConstants.L_CODES + 1];
        internal int heap_len;
        internal int heap_max;
        internal int ins_h;
        internal int last_eob_len;
        internal int last_flush;
        internal int last_lit;
        internal int lit_bufsize;
        internal int lookahead;
        internal int match_available;
        internal int match_length;
        internal int match_start;
        internal int matches;
        internal int nextPending;
        internal int opt_len;
        internal byte[] pending;
        internal int pendingCount;
        internal short[] prev;
        internal int prev_length;
        internal int prev_match;
        private bool Rfc1950BytesEmitted;
        internal int static_len;
        internal int status;
        internal int strstart;
        internal Tree treeBitLengths = new Tree();
        internal Tree treeDistances = new Tree();
        internal Tree treeLiterals = new Tree();
        internal int w_bits;
        internal int w_mask;
        internal int w_size;
        internal byte[] window;
        internal int window_size;

        internal DeflateManager()
        {
            dyn_ltree = new short[HEAP_SIZE*2];
            dyn_dtree = new short[(2*InternalConstants.D_CODES + 1)*2];
            bl_tree = new short[(2*InternalConstants.BL_CODES + 1)*2];
        }

        internal bool WantRfc1950HeaderBytes
        {
            get { return _WantRfc1950HeaderBytes; }
            set { _WantRfc1950HeaderBytes = value; }
        }

        private void _InitializeLazyMatch()
        {
            window_size = 2*w_size;
            Array.Clear(head, 0, hash_size);
            config = Config.Lookup(compressionLevel);
            SetDeflater();
            strstart = 0;
            block_start = 0;
            lookahead = 0;
            match_length = prev_length = MIN_MATCH - 1;
            match_available = 0;
            ins_h = 0;
        }

        private void _InitializeTreeData()
        {
            treeLiterals.dyn_tree = dyn_ltree;
            treeLiterals.staticTree = StaticTree.Literals;
            treeDistances.dyn_tree = dyn_dtree;
            treeDistances.staticTree = StaticTree.Distances;
            treeBitLengths.dyn_tree = bl_tree;
            treeBitLengths.staticTree = StaticTree.BitLengths;
            bi_buf = 0;
            bi_valid = 0;
            last_eob_len = 8;
            _InitializeBlocks();
        }

        internal void _InitializeBlocks()
        {
            for (var index = 0; index < InternalConstants.L_CODES; ++index)
                dyn_ltree[index*2] = 0;
            for (var index = 0; index < InternalConstants.D_CODES; ++index)
                dyn_dtree[index*2] = 0;
            for (var index = 0; index < InternalConstants.BL_CODES; ++index)
                bl_tree[index*2] = 0;
            dyn_ltree[END_BLOCK*2] = 1;
            opt_len = static_len = 0;
            last_lit = matches = 0;
        }

        internal void pqdownheap(short[] tree, int k)
        {
            var n = heap[k];
            var index = k << 1;
            while (index <= heap_len)
            {
                if (index < heap_len && _IsSmaller(tree, heap[index + 1], heap[index], depth))
                    ++index;
                if (!_IsSmaller(tree, n, heap[index], depth))
                {
                    heap[k] = heap[index];
                    k = index;
                    index <<= 1;
                }
                else
                    break;
            }
            heap[k] = n;
        }

        internal static bool _IsSmaller(short[] tree, int n, int m, sbyte[] depth)
        {
            var num1 = tree[n*2];
            var num2 = tree[m*2];
            return num1 < num2 || num1 == num2 && depth[n] <= depth[m];
        }

        internal void scan_tree(short[] tree, int max_code)
        {
            var num1 = -1;
            int num2 = tree[1];
            var num3 = 0;
            var num4 = 7;
            var num5 = 4;
            if (num2 == 0)
            {
                num4 = 138;
                num5 = 3;
            }
            tree[(max_code + 1)*2 + 1] = short.MaxValue;
            for (var index = 0; index <= max_code; ++index)
            {
                var num6 = num2;
                num2 = tree[(index + 1)*2 + 1];
                if (++num3 >= num4 || num6 != num2)
                {
                    if (num3 < num5)
                        bl_tree[num6*2] = (short) (bl_tree[num6*2] + num3);
                    else if (num6 != 0)
                    {
                        if (num6 != num1)
                            ++bl_tree[num6*2];
                        ++bl_tree[InternalConstants.REP_3_6*2];
                    }
                    else if (num3 <= 10)
                        ++bl_tree[InternalConstants.REPZ_3_10*2];
                    else
                        ++bl_tree[InternalConstants.REPZ_11_138*2];
                    num3 = 0;
                    num1 = num6;
                    if (num2 == 0)
                    {
                        num4 = 138;
                        num5 = 3;
                    }
                    else if (num6 == num2)
                    {
                        num4 = 6;
                        num5 = 3;
                    }
                    else
                    {
                        num4 = 7;
                        num5 = 4;
                    }
                }
            }
        }

        internal int build_bl_tree()
        {
            scan_tree(dyn_ltree, treeLiterals.max_code);
            scan_tree(dyn_dtree, treeDistances.max_code);
            treeBitLengths.build_tree(this);
            var index = InternalConstants.BL_CODES - 1;
            while (index >= 3 && bl_tree[Tree.bl_order[index]*2 + 1] == 0)
                --index;
            opt_len += 3*(index + 1) + 5 + 5 + 4;
            return index;
        }

        internal void send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            send_bits(lcodes - 257, 5);
            send_bits(dcodes - 1, 5);
            send_bits(blcodes - 4, 4);
            for (var index = 0; index < blcodes; ++index)
                send_bits(bl_tree[Tree.bl_order[index]*2 + 1], 3);
            send_tree(dyn_ltree, lcodes - 1);
            send_tree(dyn_dtree, dcodes - 1);
        }

        internal void send_tree(short[] tree, int max_code)
        {
            var num1 = -1;
            int num2 = tree[1];
            var num3 = 0;
            var num4 = 7;
            var num5 = 4;
            if (num2 == 0)
            {
                num4 = 138;
                num5 = 3;
            }
            for (var index = 0; index <= max_code; ++index)
            {
                var c = num2;
                num2 = tree[(index + 1)*2 + 1];
                if (++num3 >= num4 || c != num2)
                {
                    if (num3 < num5)
                    {
                        do
                        {
                            send_code(c, bl_tree);
                        } while (--num3 != 0);
                    }
                    else if (c != 0)
                    {
                        if (c != num1)
                        {
                            send_code(c, bl_tree);
                            --num3;
                        }
                        send_code(InternalConstants.REP_3_6, bl_tree);
                        send_bits(num3 - 3, 2);
                    }
                    else if (num3 <= 10)
                    {
                        send_code(InternalConstants.REPZ_3_10, bl_tree);
                        send_bits(num3 - 3, 3);
                    }
                    else
                    {
                        send_code(InternalConstants.REPZ_11_138, bl_tree);
                        send_bits(num3 - 11, 7);
                    }
                    num3 = 0;
                    num1 = c;
                    if (num2 == 0)
                    {
                        num4 = 138;
                        num5 = 3;
                    }
                    else if (c == num2)
                    {
                        num4 = 6;
                        num5 = 3;
                    }
                    else
                    {
                        num4 = 7;
                        num5 = 4;
                    }
                }
            }
        }

        private void put_bytes(byte[] p, int start, int len)
        {
            Array.Copy(p, start, pending, pendingCount, len);
            pendingCount += len;
        }

        internal void send_code(int c, short[] tree)
        {
            var index = c*2;
            send_bits(tree[index] & ushort.MaxValue, tree[index + 1] & ushort.MaxValue);
        }

        internal void send_bits(int value, int length)
        {
            var num = length;
            if (bi_valid > Buf_size - num)
            {
                bi_buf |= (short) (value << bi_valid & ushort.MaxValue);
                pending[pendingCount++] = (byte) bi_buf;
                pending[pendingCount++] = (byte) ((uint) bi_buf >> 8);
                bi_buf = (short) ((uint) value >> Buf_size - bi_valid);
                bi_valid += num - Buf_size;
            }
            else
            {
                bi_buf |= (short) (value << bi_valid & ushort.MaxValue);
                bi_valid += num;
            }
        }

        internal void _tr_align()
        {
            send_bits(STATIC_TREES << 1, 3);
            send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
            bi_flush();
            if (1 + last_eob_len + 10 - bi_valid < 9)
            {
                send_bits(STATIC_TREES << 1, 3);
                send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
                bi_flush();
            }
            last_eob_len = 7;
        }

        internal bool _tr_tally(int dist, int lc)
        {
            pending[_distanceOffset + last_lit*2] = (byte) ((uint) dist >> 8);
            pending[_distanceOffset + last_lit*2 + 1] = (byte) dist;
            pending[_lengthOffset + last_lit] = (byte) lc;
            ++last_lit;
            if (dist == 0)
            {
                ++dyn_ltree[lc*2];
            }
            else
            {
                ++matches;
                --dist;
                ++dyn_ltree[(Tree.LengthCode[lc] + InternalConstants.LITERALS + 1)*2];
                ++dyn_dtree[Tree.DistanceCode(dist)*2];
            }
            if ((last_lit & 8191) == 0 && compressionLevel > CompressionLevel.Level2)
            {
                var num1 = last_lit << 3;
                var num2 = strstart - block_start;
                for (var index = 0; index < InternalConstants.D_CODES; ++index)
                    num1 = (int) (num1 + dyn_dtree[index*2]*(5L + Tree.ExtraDistanceBits[index]));
                if (matches < last_lit/2 && num1 >> 3 < num2/2)
                    return true;
            }
            return last_lit == lit_bufsize - 1 || last_lit == lit_bufsize;
        }

        internal void send_compressed_block(short[] ltree, short[] dtree)
        {
            var num1 = 0;
            if (last_lit != 0)
            {
                do
                {
                    var index1 = _distanceOffset + num1*2;
                    var num2 = pending[index1] << 8 & 65280 | pending[index1 + 1] & byte.MaxValue;
                    var c1 = pending[_lengthOffset + num1] & byte.MaxValue;
                    ++num1;
                    if (num2 == 0)
                    {
                        send_code(c1, ltree);
                    }
                    else
                    {
                        int index2 = Tree.LengthCode[c1];
                        send_code(index2 + InternalConstants.LITERALS + 1, ltree);
                        var length1 = Tree.ExtraLengthBits[index2];
                        if (length1 != 0)
                            send_bits(c1 - Tree.LengthBase[index2], length1);
                        var dist = num2 - 1;
                        var c2 = Tree.DistanceCode(dist);
                        send_code(c2, dtree);
                        var length2 = Tree.ExtraDistanceBits[c2];
                        if (length2 != 0)
                            send_bits(dist - Tree.DistanceBase[c2], length2);
                    }
                } while (num1 < last_lit);
            }
            send_code(END_BLOCK, ltree);
            last_eob_len = ltree[END_BLOCK*2 + 1];
        }

        internal void set_data_type()
        {
            var num1 = 0;
            var num2 = 0;
            var num3 = 0;
            for (; num1 < 7; ++num1)
                num3 += dyn_ltree[num1*2];
            for (; num1 < 128; ++num1)
                num2 += dyn_ltree[num1*2];
            for (; num1 < InternalConstants.LITERALS; ++num1)
                num3 += dyn_ltree[num1*2];
            data_type = num3 > num2 >> 2 ? (sbyte) Z_BINARY : (sbyte) Z_ASCII;
        }

        internal void bi_flush()
        {
            if (bi_valid == 16)
            {
                pending[pendingCount++] = (byte) bi_buf;
                pending[pendingCount++] = (byte) ((uint) bi_buf >> 8);
                bi_buf = 0;
                bi_valid = 0;
            }
            else
            {
                if (bi_valid < 8)
                    return;
                pending[pendingCount++] = (byte) bi_buf;
                bi_buf >>= 8;
                bi_valid -= 8;
            }
        }

        internal void bi_windup()
        {
            if (bi_valid > 8)
            {
                pending[pendingCount++] = (byte) bi_buf;
                pending[pendingCount++] = (byte) ((uint) bi_buf >> 8);
            }
            else if (bi_valid > 0)
                pending[pendingCount++] = (byte) bi_buf;
            bi_buf = 0;
            bi_valid = 0;
        }

        internal void copy_block(int buf, int len, bool header)
        {
            bi_windup();
            last_eob_len = 8;
            if (header)
            {
                pending[pendingCount++] = (byte) len;
                pending[pendingCount++] = (byte) (len >> 8);
                pending[pendingCount++] = (byte) ~len;
                pending[pendingCount++] = (byte) (~len >> 8);
            }
            put_bytes(window, buf, len);
        }

        internal void flush_block_only(bool eof)
        {
            _tr_flush_block(block_start >= 0 ? block_start : -1, strstart - block_start, eof);
            block_start = strstart;
            _codec.flush_pending();
        }

        internal BlockState DeflateNone(FlushType flush)
        {
            int num1 = ushort.MaxValue;
            if (num1 > pending.Length - 5)
                num1 = pending.Length - 5;
            while (true)
            {
                if (lookahead <= 1)
                {
                    _fillWindow();
                    if (lookahead != 0 || flush != FlushType.None)
                    {
                        if (lookahead == 0)
                            goto label_13;
                    }
                    else
                        break;
                }
                strstart += lookahead;
                lookahead = 0;
                var num2 = block_start + num1;
                if (strstart == 0 || strstart >= num2)
                {
                    lookahead = strstart - num2;
                    strstart = num2;
                    flush_block_only(false);
                    if (_codec.AvailableBytesOut == 0)
                        goto label_7;
                }
                if (strstart - block_start >= w_size - MIN_LOOKAHEAD)
                {
                    flush_block_only(false);
                    if (_codec.AvailableBytesOut == 0)
                        goto label_10;
                }
            }
            return BlockState.NeedMore;
            label_7:
            return BlockState.NeedMore;
            label_10:
            return BlockState.NeedMore;
            label_13:
            flush_block_only(flush == FlushType.Finish);
            if (_codec.AvailableBytesOut == 0)
                return flush == FlushType.Finish ? BlockState.FinishStarted : BlockState.NeedMore;
            return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
        }

        internal void _tr_stored_block(int buf, int stored_len, bool eof)
        {
            send_bits((STORED_BLOCK << 1) + (eof ? 1 : 0), 3);
            copy_block(buf, stored_len, true);
        }

        internal void _tr_flush_block(int buf, int stored_len, bool eof)
        {
            var num1 = 0;
            int num2;
            int num3;
            if (compressionLevel > CompressionLevel.None)
            {
                if (data_type == Z_UNKNOWN)
                    set_data_type();
                treeLiterals.build_tree(this);
                treeDistances.build_tree(this);
                num1 = build_bl_tree();
                num2 = opt_len + 3 + 7 >> 3;
                num3 = static_len + 3 + 7 >> 3;
                if (num3 <= num2)
                    num2 = num3;
            }
            else
                num2 = num3 = stored_len + 5;
            if (stored_len + 4 <= num2 && buf != -1)
                _tr_stored_block(buf, stored_len, eof);
            else if (num3 == num2)
            {
                send_bits((STATIC_TREES << 1) + (eof ? 1 : 0), 3);
                send_compressed_block(StaticTree.lengthAndLiteralsTreeCodes, StaticTree.distTreeCodes);
            }
            else
            {
                send_bits((DYN_TREES << 1) + (eof ? 1 : 0), 3);
                send_all_trees(treeLiterals.max_code + 1, treeDistances.max_code + 1, num1 + 1);
                send_compressed_block(dyn_ltree, dyn_dtree);
            }
            _InitializeBlocks();
            if (!eof)
                return;
            bi_windup();
        }

        private void _fillWindow()
        {
            do
            {
                var size = window_size - lookahead - strstart;
                if (size == 0 && strstart == 0 && lookahead == 0)
                    size = w_size;
                else if (size == -1)
                    --size;
                else if (strstart >= w_size + w_size - MIN_LOOKAHEAD)
                {
                    Array.Copy(window, w_size, window, 0, w_size);
                    match_start -= w_size;
                    strstart -= w_size;
                    block_start -= w_size;
                    var num1 = hash_size;
                    var index1 = num1;
                    do
                    {
                        var num2 = head[--index1] & ushort.MaxValue;
                        head[index1] = num2 >= w_size ? (short) (num2 - w_size) : (short) 0;
                    } while (--num1 != 0);
                    var num3 = w_size;
                    var index2 = num3;
                    do
                    {
                        var num2 = prev[--index2] & ushort.MaxValue;
                        prev[index2] = num2 >= w_size ? (short) (num2 - w_size) : (short) 0;
                    } while (--num3 != 0);
                    size += w_size;
                }
                if (_codec.AvailableBytesIn != 0)
                {
                    lookahead += _codec.read_buf(window, strstart + lookahead, size);
                    if (lookahead >= MIN_MATCH)
                    {
                        ins_h = window[strstart] & byte.MaxValue;
                        ins_h = (ins_h << hash_shift ^ window[strstart + 1] & byte.MaxValue) & hash_mask;
                    }
                }
                else
                    goto label_16;
            } while (lookahead < MIN_LOOKAHEAD && _codec.AvailableBytesIn != 0);
            goto label_12;
            label_16:
            return;
            label_12:
            ;
        }

        internal BlockState DeflateFast(FlushType flush)
        {
            var cur_match = 0;
            while (true)
            {
                if (lookahead < MIN_LOOKAHEAD)
                {
                    _fillWindow();
                    if (lookahead >= MIN_LOOKAHEAD || flush != FlushType.None)
                    {
                        if (lookahead == 0)
                            goto label_20;
                    }
                    else
                        break;
                }
                if (lookahead >= MIN_MATCH)
                {
                    ins_h = (ins_h << hash_shift ^ window[strstart + (MIN_MATCH - 1)] & byte.MaxValue) & hash_mask;
                    cur_match = head[ins_h] & ushort.MaxValue;
                    prev[strstart & w_mask] = head[ins_h];
                    head[ins_h] = (short) strstart;
                }
                if (cur_match != 0L && (strstart - cur_match & ushort.MaxValue) <= w_size - MIN_LOOKAHEAD &&
                    compressionStrategy != CompressionStrategy.HuffmanOnly)
                    match_length = longest_match(cur_match);
                bool flag;
                if (match_length >= MIN_MATCH)
                {
                    flag = _tr_tally(strstart - match_start, match_length - MIN_MATCH);
                    lookahead -= match_length;
                    if (match_length <= config.MaxLazy && lookahead >= MIN_MATCH)
                    {
                        --match_length;
                        do
                        {
                            ++strstart;
                            ins_h = (ins_h << hash_shift ^ window[strstart + (MIN_MATCH - 1)] & byte.MaxValue) & hash_mask;
                            cur_match = head[ins_h] & ushort.MaxValue;
                            prev[strstart & w_mask] = head[ins_h];
                            head[ins_h] = (short) strstart;
                        } while (--match_length != 0);
                        ++strstart;
                    }
                    else
                    {
                        strstart += match_length;
                        match_length = 0;
                        ins_h = window[strstart] & byte.MaxValue;
                        ins_h = (ins_h << hash_shift ^ window[strstart + 1] & byte.MaxValue) & hash_mask;
                    }
                }
                else
                {
                    flag = _tr_tally(0, window[strstart] & byte.MaxValue);
                    --lookahead;
                    ++strstart;
                }
                if (flag)
                {
                    flush_block_only(false);
                    if (_codec.AvailableBytesOut == 0)
                        goto label_17;
                }
            }
            return BlockState.NeedMore;
            label_17:
            return BlockState.NeedMore;
            label_20:
            flush_block_only(flush == FlushType.Finish);
            if (_codec.AvailableBytesOut != 0)
                return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
            return flush == FlushType.Finish ? BlockState.FinishStarted : BlockState.NeedMore;
        }

        internal BlockState DeflateSlow(FlushType flush)
        {
            var cur_match = 0;
            while (true)
            {
                if (lookahead < MIN_LOOKAHEAD)
                {
                    _fillWindow();
                    if (lookahead >= MIN_LOOKAHEAD || flush != FlushType.None)
                    {
                        if (lookahead == 0)
                            goto label_28;
                    }
                    else
                        break;
                }
                if (lookahead >= MIN_MATCH)
                {
                    ins_h = (ins_h << hash_shift ^ window[strstart + (MIN_MATCH - 1)] & byte.MaxValue) & hash_mask;
                    cur_match = head[ins_h] & ushort.MaxValue;
                    prev[strstart & w_mask] = head[ins_h];
                    head[ins_h] = (short) strstart;
                }
                prev_length = match_length;
                prev_match = match_start;
                match_length = MIN_MATCH - 1;
                if (cur_match != 0 && prev_length < config.MaxLazy && (strstart - cur_match & ushort.MaxValue) <= w_size - MIN_LOOKAHEAD)
                {
                    if (compressionStrategy != CompressionStrategy.HuffmanOnly)
                        match_length = longest_match(cur_match);
                    if (match_length <= 5 &&
                        (compressionStrategy == CompressionStrategy.Filtered || match_length == MIN_MATCH && strstart - match_start > 4096))
                        match_length = MIN_MATCH - 1;
                }
                if (prev_length >= MIN_MATCH && match_length <= prev_length)
                {
                    var num = strstart + lookahead - MIN_MATCH;
                    var flag = _tr_tally(strstart - 1 - prev_match, prev_length - MIN_MATCH);
                    lookahead -= prev_length - 1;
                    prev_length -= 2;
                    do
                    {
                        if (++strstart <= num)
                        {
                            ins_h = (ins_h << hash_shift ^ window[strstart + (MIN_MATCH - 1)] & byte.MaxValue) & hash_mask;
                            cur_match = head[ins_h] & ushort.MaxValue;
                            prev[strstart & w_mask] = head[ins_h];
                            head[ins_h] = (short) strstart;
                        }
                    } while (--prev_length != 0);
                    match_available = 0;
                    match_length = MIN_MATCH - 1;
                    ++strstart;
                    if (flag)
                    {
                        flush_block_only(false);
                        if (_codec.AvailableBytesOut == 0)
                            goto label_19;
                    }
                }
                else if (match_available != 0)
                {
                    if (_tr_tally(0, window[strstart - 1] & byte.MaxValue))
                        flush_block_only(false);
                    ++strstart;
                    --lookahead;
                    if (_codec.AvailableBytesOut == 0)
                        goto label_24;
                }
                else
                {
                    match_available = 1;
                    ++strstart;
                    --lookahead;
                }
            }
            return BlockState.NeedMore;
            label_19:
            return BlockState.NeedMore;
            label_24:
            return BlockState.NeedMore;
            label_28:
            if (match_available != 0)
            {
                _tr_tally(0, window[strstart - 1] & byte.MaxValue);
                match_available = 0;
            }
            flush_block_only(flush == FlushType.Finish);
            if (_codec.AvailableBytesOut != 0)
                return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
            return flush == FlushType.Finish ? BlockState.FinishStarted : BlockState.NeedMore;
        }

        internal int longest_match(int cur_match)
        {
            var num1 = config.MaxChainLength;
            var index1 = strstart;
            var num2 = prev_length;
            var num3 = strstart > w_size - MIN_LOOKAHEAD ? strstart - (w_size - MIN_LOOKAHEAD) : 0;
            var num4 = config.NiceLength;
            var num5 = w_mask;
            var num6 = strstart + MAX_MATCH;
            var num7 = window[index1 + num2 - 1];
            var num8 = window[index1 + num2];
            if (prev_length >= config.GoodLength)
                num1 >>= 2;
            if (num4 > lookahead)
                num4 = lookahead;
            do
            {
                var index2 = cur_match;
                if (window[index2 + num2] == num8 && window[index2 + num2 - 1] == num7 && window[index2] == window[index1] &&
                    window[++index2] == window[index1 + 1])
                {
                    var num9 = index1 + 2;
                    var num10 = index2 + 1;
                    do
                        ; while (window[++num9] == window[++num10] && window[++num9] == window[++num10] &&
                                 (window[++num9] == window[++num10] && window[++num9] == window[++num10]) &&
                                 (window[++num9] == window[++num10] && window[++num9] == window[++num10] &&
                                  (window[++num9] == window[++num10] && window[++num9] == window[++num10])) && num9 < num6);
                    var num11 = MAX_MATCH - (num6 - num9);
                    index1 = num6 - MAX_MATCH;
                    if (num11 > num2)
                    {
                        match_start = cur_match;
                        num2 = num11;
                        if (num11 < num4)
                        {
                            num7 = window[index1 + num2 - 1];
                            num8 = window[index1 + num2];
                        }
                        else
                            break;
                    }
                }
            } while ((cur_match = prev[cur_match & num5] & ushort.MaxValue) > num3 && --num1 != 0);
            if (num2 <= lookahead)
                return num2;
            return lookahead;
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level)
        {
            return Initialize(codec, level, 15);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits)
        {
            return Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, CompressionStrategy.Default);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
        {
            return Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, compressionStrategy);
        }

        internal int Initialize(ZlibCodec codec, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
        {
            _codec = codec;
            _codec.Message = null;
            if (windowBits < 9 || windowBits > 15)
                throw new ZlibException("windowBits must be in the range 9..15.");
            if (memLevel < 1 || memLevel > MEM_LEVEL_MAX)
                throw new ZlibException(string.Format("memLevel must be in the range 1.. {0}", MEM_LEVEL_MAX));
            _codec.dstate = this;
            w_bits = windowBits;
            w_size = 1 << w_bits;
            w_mask = w_size - 1;
            hash_bits = memLevel + 7;
            hash_size = 1 << hash_bits;
            hash_mask = hash_size - 1;
            hash_shift = (hash_bits + MIN_MATCH - 1)/MIN_MATCH;
            window = new byte[w_size*2];
            prev = new short[w_size];
            head = new short[hash_size];
            lit_bufsize = 1 << memLevel + 6;
            pending = new byte[lit_bufsize*4];
            _distanceOffset = lit_bufsize;
            _lengthOffset = 3*lit_bufsize;
            compressionLevel = level;
            compressionStrategy = strategy;
            Reset();
            return 0;
        }

        internal void Reset()
        {
            _codec.TotalBytesIn = _codec.TotalBytesOut = 0L;
            _codec.Message = null;
            pendingCount = 0;
            nextPending = 0;
            Rfc1950BytesEmitted = false;
            status = WantRfc1950HeaderBytes ? INIT_STATE : BUSY_STATE;
            _codec._Adler32 = Adler.Adler32(0U, null, 0, 0);
            last_flush = 0;
            _InitializeTreeData();
            _InitializeLazyMatch();
        }

        internal int End()
        {
            if (status != INIT_STATE && status != BUSY_STATE && status != FINISH_STATE)
                return -2;
            pending = null;
            head = null;
            prev = null;
            window = null;
            return status == BUSY_STATE ? -3 : 0;
        }

        private void SetDeflater()
        {
            switch (config.Flavor)
            {
                case DeflateFlavor.Store:
                    DeflateFunction = DeflateNone;
                    break;
                case DeflateFlavor.Fast:
                    DeflateFunction = DeflateFast;
                    break;
                case DeflateFlavor.Slow:
                    DeflateFunction = DeflateSlow;
                    break;
            }
        }

        internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
        {
            var num = 0;
            if (compressionLevel != level)
            {
                var config = Config.Lookup(level);
                if (config.Flavor != this.config.Flavor && _codec.TotalBytesIn != 0L)
                    num = _codec.Deflate(FlushType.Partial);
                compressionLevel = level;
                this.config = config;
                SetDeflater();
            }
            compressionStrategy = strategy;
            return num;
        }

        internal int SetDictionary(byte[] dictionary)
        {
            var length = dictionary.Length;
            var sourceIndex = 0;
            if (dictionary == null || status != INIT_STATE)
                throw new ZlibException("Stream error.");
            _codec._Adler32 = Adler.Adler32(_codec._Adler32, dictionary, 0, dictionary.Length);
            if (length < MIN_MATCH)
                return 0;
            if (length > w_size - MIN_LOOKAHEAD)
            {
                length = w_size - MIN_LOOKAHEAD;
                sourceIndex = dictionary.Length - length;
            }
            Array.Copy(dictionary, sourceIndex, window, 0, length);
            strstart = length;
            block_start = length;
            ins_h = window[0] & byte.MaxValue;
            ins_h = (ins_h << hash_shift ^ window[1] & byte.MaxValue) & hash_mask;
            for (var index = 0; index <= length - MIN_MATCH; ++index)
            {
                ins_h = (ins_h << hash_shift ^ window[index + (MIN_MATCH - 1)] & byte.MaxValue) & hash_mask;
                prev[index & w_mask] = head[ins_h];
                head[ins_h] = (short) index;
            }
            return 0;
        }

        internal int Deflate(FlushType flush)
        {
            if (_codec.OutputBuffer == null || _codec.InputBuffer == null && _codec.AvailableBytesIn != 0 ||
                status == FINISH_STATE && flush != FlushType.Finish)
            {
                _codec.Message = _ErrorMessage[4];
                throw new ZlibException(string.Format("Something is fishy. [{0}]", _codec.Message));
            }
            if (_codec.AvailableBytesOut == 0)
            {
                _codec.Message = _ErrorMessage[7];
                throw new ZlibException("OutputBuffer is full (AvailableBytesOut == 0)");
            }
            var num1 = last_flush;
            last_flush = (int) flush;
            if (status == INIT_STATE)
            {
                var num2 = Z_DEFLATED + (w_bits - 8 << 4) << 8;
                var num3 = (int) (compressionLevel - 1 & (CompressionLevel) 255) >> 1;
                if (num3 > 3)
                    num3 = 3;
                var num4 = num2 | num3 << 6;
                if (strstart != 0)
                    num4 |= PRESET_DICT;
                var num5 = num4 + (31 - num4%31);
                status = BUSY_STATE;
                pending[pendingCount++] = (byte) (num5 >> 8);
                pending[pendingCount++] = (byte) num5;
                if (strstart != 0)
                {
                    pending[pendingCount++] = (byte) ((_codec._Adler32 & 4278190080U) >> 24);
                    pending[pendingCount++] = (byte) ((_codec._Adler32 & 16711680U) >> 16);
                    pending[pendingCount++] = (byte) ((_codec._Adler32 & 65280U) >> 8);
                    pending[pendingCount++] = (byte) (_codec._Adler32 & byte.MaxValue);
                }
                _codec._Adler32 = Adler.Adler32(0U, null, 0, 0);
            }
            if (pendingCount != 0)
            {
                _codec.flush_pending();
                if (_codec.AvailableBytesOut == 0)
                {
                    last_flush = -1;
                    return 0;
                }
            }
            else if (_codec.AvailableBytesIn == 0 && flush <= (FlushType) num1 && flush != FlushType.Finish)
                return 0;
            if (status == FINISH_STATE && _codec.AvailableBytesIn != 0)
            {
                _codec.Message = _ErrorMessage[7];
                throw new ZlibException("status == FINISH_STATE && _codec.AvailableBytesIn != 0");
            }
            if (_codec.AvailableBytesIn != 0 || lookahead != 0 || flush != FlushType.None && status != FINISH_STATE)
            {
                var blockState = DeflateFunction(flush);
                if (blockState == BlockState.FinishStarted || blockState == BlockState.FinishDone)
                    status = FINISH_STATE;
                if (blockState == BlockState.NeedMore || blockState == BlockState.FinishStarted)
                {
                    if (_codec.AvailableBytesOut == 0)
                        last_flush = -1;
                    return 0;
                }
                if (blockState == BlockState.BlockDone)
                {
                    if (flush == FlushType.Partial)
                    {
                        _tr_align();
                    }
                    else
                    {
                        _tr_stored_block(0, 0, false);
                        if (flush == FlushType.Full)
                        {
                            for (var index = 0; index < hash_size; ++index)
                                head[index] = 0;
                        }
                    }
                    _codec.flush_pending();
                    if (_codec.AvailableBytesOut == 0)
                    {
                        last_flush = -1;
                        return 0;
                    }
                }
            }
            if (flush != FlushType.Finish)
                return 0;
            if (!WantRfc1950HeaderBytes || Rfc1950BytesEmitted)
                return 1;
            pending[pendingCount++] = (byte) ((_codec._Adler32 & 4278190080U) >> 24);
            pending[pendingCount++] = (byte) ((_codec._Adler32 & 16711680U) >> 16);
            pending[pendingCount++] = (byte) ((_codec._Adler32 & 65280U) >> 8);
            pending[pendingCount++] = (byte) (_codec._Adler32 & byte.MaxValue);
            _codec.flush_pending();
            Rfc1950BytesEmitted = true;
            return pendingCount != 0 ? 0 : 1;
        }

        internal delegate BlockState CompressFunc(FlushType flush);

        internal class Config
        {
            private static readonly Config[] Table = new Config[10]
            {
                new Config(0, 0, 0, 0, DeflateFlavor.Store),
                new Config(4, 4, 8, 4, DeflateFlavor.Fast),
                new Config(4, 5, 16, 8, DeflateFlavor.Fast),
                new Config(4, 6, 32, 32, DeflateFlavor.Fast),
                new Config(4, 4, 16, 16, DeflateFlavor.Slow),
                new Config(8, 16, 32, 32, DeflateFlavor.Slow),
                new Config(8, 16, 128, 128, DeflateFlavor.Slow),
                new Config(8, 32, 128, 256, DeflateFlavor.Slow),
                new Config(32, 128, 258, 1024, DeflateFlavor.Slow),
                new Config(32, 258, 258, 4096, DeflateFlavor.Slow)
            };

            internal DeflateFlavor Flavor;
            internal int GoodLength;
            internal int MaxChainLength;
            internal int MaxLazy;
            internal int NiceLength;

            private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
            {
                GoodLength = goodLength;
                MaxLazy = maxLazy;
                NiceLength = niceLength;
                MaxChainLength = maxChainLength;
                Flavor = flavor;
            }

            public static Config Lookup(CompressionLevel level)
            {
                return Table[(int) level];
            }
        }
    }
}