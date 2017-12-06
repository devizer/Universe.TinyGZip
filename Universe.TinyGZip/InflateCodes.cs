namespace Universe.TinyGZip.InternalImplementation
{
    using System;

    #pragma warning disable 642, 219
    internal sealed class InflateCodes
    {
        private const int START = 0;
        private const int LEN = 1;
        private const int LENEXT = 2;
        private const int DIST = 3;
        private const int DISTEXT = 4;
        private const int COPY = 5;
        private const int LIT = 6;
        private const int WASH = 7;
        private const int END = 8;
        private const int BADCODE = 9;
        internal int bitsToGet;
        internal byte dbits;
        internal int dist;
        internal int[] dtree;
        internal int dtree_index;
        internal byte lbits;
        internal int len;
        internal int lit;
        internal int[] ltree;
        internal int ltree_index;
        internal int mode;
        internal int need;
        internal int[] tree;
        internal int tree_index;

        internal void Init(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index)
        {
            mode = 0;
            lbits = (byte) bl;
            dbits = (byte) bd;
            ltree = tl;
            ltree_index = tl_index;
            dtree = td;
            dtree_index = td_index;
            tree = null;
        }

        internal int Process(InflateBlocks blocks, int r)
        {
            var z = blocks._codec;
            var num1 = z.NextIn;
            var num2 = z.AvailableBytesIn;
            var num3 = blocks.bitb;
            var num4 = blocks.bitk;
            var num5 = blocks.writeAt;
            var num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
            while (true)
            {
                switch (mode)
                {
                    case 0:
                        if (num6 >= 258 && num2 >= 10)
                        {
                            blocks.bitb = num3;
                            blocks.bitk = num4;
                            z.AvailableBytesIn = num2;
                            z.TotalBytesIn += num1 - z.NextIn;
                            z.NextIn = num1;
                            blocks.writeAt = num5;
                            r = InflateFast(lbits, dbits, ltree, ltree_index, dtree, dtree_index, blocks, z);
                            num1 = z.NextIn;
                            num2 = z.AvailableBytesIn;
                            num3 = blocks.bitb;
                            num4 = blocks.bitk;
                            num5 = blocks.writeAt;
                            num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                            if (r != 0)
                            {
                                mode = r == 1 ? 7 : 9;
                                break;
                            }
                        }
                        need = lbits;
                        tree = ltree;
                        tree_index = ltree_index;
                        mode = 1;
                        goto case 1;
                    case 1:
                        var index1 = need;
                        while (num4 < index1)
                        {
                            if (num2 != 0)
                            {
                                r = 0;
                                --num2;
                                num3 |= (z.InputBuffer[num1++] & byte.MaxValue) << num4;
                                num4 += 8;
                            }
                            else
                            {
                                blocks.bitb = num3;
                                blocks.bitk = num4;
                                z.AvailableBytesIn = num2;
                                z.TotalBytesIn += num1 - z.NextIn;
                                z.NextIn = num1;
                                blocks.writeAt = num5;
                                return blocks.Flush(r);
                            }
                        }
                        var index2 = (tree_index + (num3 & InternalInflateConstants.InflateMask[index1]))*3;
                        num3 >>= tree[index2 + 1];
                        num4 -= tree[index2 + 1];
                        var num7 = tree[index2];
                        if (num7 == 0)
                        {
                            lit = tree[index2 + 2];
                            mode = 6;
                            break;
                        }
                        if ((num7 & 16) != 0)
                        {
                            bitsToGet = num7 & 15;
                            len = tree[index2 + 2];
                            mode = 2;
                            break;
                        }
                        if ((num7 & 64) == 0)
                        {
                            need = num7;
                            tree_index = index2/3 + tree[index2 + 2];
                            break;
                        }
                        if ((num7 & 32) != 0)
                        {
                            mode = 7;
                            break;
                        }
                        goto label_18;
                    case 2:
                        var index3 = bitsToGet;
                        while (num4 < index3)
                        {
                            if (num2 != 0)
                            {
                                r = 0;
                                --num2;
                                num3 |= (z.InputBuffer[num1++] & byte.MaxValue) << num4;
                                num4 += 8;
                            }
                            else
                            {
                                blocks.bitb = num3;
                                blocks.bitk = num4;
                                z.AvailableBytesIn = num2;
                                z.TotalBytesIn += num1 - z.NextIn;
                                z.NextIn = num1;
                                blocks.writeAt = num5;
                                return blocks.Flush(r);
                            }
                        }
                        len += num3 & InternalInflateConstants.InflateMask[index3];
                        num3 >>= index3;
                        num4 -= index3;
                        need = dbits;
                        tree = dtree;
                        tree_index = dtree_index;
                        mode = 3;
                        goto case 3;
                    case 3:
                        var index4 = need;
                        while (num4 < index4)
                        {
                            if (num2 != 0)
                            {
                                r = 0;
                                --num2;
                                num3 |= (z.InputBuffer[num1++] & byte.MaxValue) << num4;
                                num4 += 8;
                            }
                            else
                            {
                                blocks.bitb = num3;
                                blocks.bitk = num4;
                                z.AvailableBytesIn = num2;
                                z.TotalBytesIn += num1 - z.NextIn;
                                z.NextIn = num1;
                                blocks.writeAt = num5;
                                return blocks.Flush(r);
                            }
                        }
                        var index5 = (tree_index + (num3 & InternalInflateConstants.InflateMask[index4]))*3;
                        num3 >>= tree[index5 + 1];
                        num4 -= tree[index5 + 1];
                        var num8 = tree[index5];
                        if ((num8 & 16) != 0)
                        {
                            bitsToGet = num8 & 15;
                            dist = tree[index5 + 2];
                            mode = 4;
                            break;
                        }
                        if ((num8 & 64) == 0)
                        {
                            need = num8;
                            tree_index = index5/3 + tree[index5 + 2];
                            break;
                        }
                        goto label_34;
                    case 4:
                        var index6 = bitsToGet;
                        while (num4 < index6)
                        {
                            if (num2 != 0)
                            {
                                r = 0;
                                --num2;
                                num3 |= (z.InputBuffer[num1++] & byte.MaxValue) << num4;
                                num4 += 8;
                            }
                            else
                            {
                                blocks.bitb = num3;
                                blocks.bitk = num4;
                                z.AvailableBytesIn = num2;
                                z.TotalBytesIn += num1 - z.NextIn;
                                z.NextIn = num1;
                                blocks.writeAt = num5;
                                return blocks.Flush(r);
                            }
                        }
                        dist += num3 & InternalInflateConstants.InflateMask[index6];
                        num3 >>= index6;
                        num4 -= index6;
                        mode = 5;
                        goto case 5;
                    case 5:
                        var num9 = num5 - dist;
                        while (num9 < 0)
                            num9 += blocks.end;
                        for (; len != 0; --len)
                        {
                            if (num6 == 0)
                            {
                                if (num5 == blocks.end && blocks.readAt != 0)
                                {
                                    num5 = 0;
                                    num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                                }
                                if (num6 == 0)
                                {
                                    blocks.writeAt = num5;
                                    r = blocks.Flush(r);
                                    num5 = blocks.writeAt;
                                    num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                                    if (num5 == blocks.end && blocks.readAt != 0)
                                    {
                                        num5 = 0;
                                        num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                                    }
                                    if (num6 == 0)
                                    {
                                        blocks.bitb = num3;
                                        blocks.bitk = num4;
                                        z.AvailableBytesIn = num2;
                                        z.TotalBytesIn += num1 - z.NextIn;
                                        z.NextIn = num1;
                                        blocks.writeAt = num5;
                                        return blocks.Flush(r);
                                    }
                                }
                            }
                            blocks.window[num5++] = blocks.window[num9++];
                            --num6;
                            if (num9 == blocks.end)
                                num9 = 0;
                        }
                        mode = 0;
                        break;
                    case 6:
                        if (num6 == 0)
                        {
                            if (num5 == blocks.end && blocks.readAt != 0)
                            {
                                num5 = 0;
                                num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                            }
                            if (num6 == 0)
                            {
                                blocks.writeAt = num5;
                                r = blocks.Flush(r);
                                num5 = blocks.writeAt;
                                num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                                if (num5 == blocks.end && blocks.readAt != 0)
                                {
                                    num5 = 0;
                                    num6 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
                                }
                                if (num6 == 0)
                                    goto label_65;
                            }
                        }
                        r = 0;
                        blocks.window[num5++] = (byte) lit;
                        --num6;
                        mode = 0;
                        break;
                    case 7:
                        goto label_68;
                    case 8:
                        goto label_73;
                    case 9:
                        goto label_74;
                    default:
                        goto label_75;
                }
            }
            label_18:
            mode = 9;
            z.Message = "invalid literal/length code";
            r = -3;
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
            label_34:
            mode = 9;
            z.Message = "invalid distance code";
            r = -3;
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
            label_65:
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
            label_68:
            if (num4 > 7)
            {
                num4 -= 8;
                ++num2;
                --num1;
            }
            blocks.writeAt = num5;
            r = blocks.Flush(r);
            num5 = blocks.writeAt;
            var num10 = num5 < blocks.readAt ? blocks.readAt - num5 - 1 : blocks.end - num5;
            if (blocks.readAt != blocks.writeAt)
            {
                blocks.bitb = num3;
                blocks.bitk = num4;
                z.AvailableBytesIn = num2;
                z.TotalBytesIn += num1 - z.NextIn;
                z.NextIn = num1;
                blocks.writeAt = num5;
                return blocks.Flush(r);
            }
            mode = 8;
            label_73:
            r = 1;
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
            label_74:
            r = -3;
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
            label_75:
            r = -2;
            blocks.bitb = num3;
            blocks.bitk = num4;
            z.AvailableBytesIn = num2;
            z.TotalBytesIn += num1 - z.NextIn;
            z.NextIn = num1;
            blocks.writeAt = num5;
            return blocks.Flush(r);
        }

        internal int InflateFast(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, InflateBlocks s, ZlibCodec z)
        {
            var num1 = z.NextIn;
            var num2 = z.AvailableBytesIn;
            var num3 = s.bitb;
            var num4 = s.bitk;
            var destinationIndex = s.writeAt;
            var num5 = destinationIndex < s.readAt ? s.readAt - destinationIndex - 1 : s.end - destinationIndex;
            var num6 = InternalInflateConstants.InflateMask[bl];
            var num7 = InternalInflateConstants.InflateMask[bd];
            do
            {
                while (num4 < 20)
                {
                    --num2;
                    num3 |= (z.InputBuffer[num1++] & byte.MaxValue) << num4;
                    num4 += 8;
                }
                var num8 = num3 & num6;
                var numArray1 = tl;
                var num9 = tl_index;
                var index1 = (num9 + num8)*3;
                int index2;
                if ((index2 = numArray1[index1]) == 0)
                {
                    num3 >>= numArray1[index1 + 1];
                    num4 -= numArray1[index1 + 1];
                    s.window[destinationIndex++] = (byte) numArray1[index1 + 2];
                    --num5;
                }
                else
                {
                    bool flag;
                    while (true)
                    {
                        num3 >>= numArray1[index1 + 1];
                        num4 -= numArray1[index1 + 1];
                        if ((index2 & 16) == 0)
                        {
                            if ((index2 & 64) == 0)
                            {
                                num8 = num8 + numArray1[index1 + 2] + (num3 & InternalInflateConstants.InflateMask[index2]);
                                index1 = (num9 + num8)*3;
                                if ((index2 = numArray1[index1]) != 0)
                                    flag = true;
                                else
                                    goto label_34;
                            }
                            else
                                goto label_35;
                        }
                        else
                            break;
                    }
                    var index3 = index2 & 15;
                    var length1 = numArray1[index1 + 2] + (num3 & InternalInflateConstants.InflateMask[index3]);
                    var num10 = num3 >> index3;
                    var num11 = num4 - index3;
                    while (num11 < 15)
                    {
                        --num2;
                        num10 |= (z.InputBuffer[num1++] & byte.MaxValue) << num11;
                        num11 += 8;
                    }
                    var num12 = num10 & num7;
                    var numArray2 = td;
                    var num13 = td_index;
                    var index4 = (num13 + num12)*3;
                    var index5 = numArray2[index4];
                    while (true)
                    {
                        num10 >>= numArray2[index4 + 1];
                        num11 -= numArray2[index4 + 1];
                        if ((index5 & 16) == 0)
                        {
                            if ((index5 & 64) == 0)
                            {
                                num12 = num12 + numArray2[index4 + 2] + (num10 & InternalInflateConstants.InflateMask[index5]);
                                index4 = (num13 + num12)*3;
                                index5 = numArray2[index4];
                                flag = true;
                            }
                            else
                                goto label_31;
                        }
                        else
                            break;
                    }
                    var index6 = index5 & 15;
                    while (num11 < index6)
                    {
                        --num2;
                        num10 |= (z.InputBuffer[num1++] & byte.MaxValue) << num11;
                        num11 += 8;
                    }
                    var num14 = numArray2[index4 + 2] + (num10 & InternalInflateConstants.InflateMask[index6]);
                    num3 = num10 >> index6;
                    num4 = num11 - index6;
                    num5 -= length1;
                    int sourceIndex1;
                    int num15;
                    if (destinationIndex >= num14)
                    {
                        var sourceIndex2 = destinationIndex - num14;
                        if (destinationIndex - sourceIndex2 > 0 && 2 > destinationIndex - sourceIndex2)
                        {
                            var numArray3 = s.window;
                            var index7 = destinationIndex;
                            var num16 = 1;
                            var num17 = index7 + num16;
                            var numArray4 = s.window;
                            var index8 = sourceIndex2;
                            var num18 = 1;
                            var num19 = index8 + num18;
                            int num20 = numArray4[index8];
                            numArray3[index7] = (byte) num20;
                            var numArray5 = s.window;
                            var index9 = num17;
                            var num21 = 1;
                            destinationIndex = index9 + num21;
                            var numArray6 = s.window;
                            var index10 = num19;
                            var num22 = 1;
                            sourceIndex1 = index10 + num22;
                            int num23 = numArray6[index10];
                            numArray5[index9] = (byte) num23;
                            length1 -= 2;
                        }
                        else
                        {
                            Array.Copy(s.window, sourceIndex2, s.window, destinationIndex, 2);
                            destinationIndex += 2;
                            sourceIndex1 = sourceIndex2 + 2;
                            length1 -= 2;
                        }
                    }
                    else
                    {
                        sourceIndex1 = destinationIndex - num14;
                        do
                        {
                            sourceIndex1 += s.end;
                        } while (sourceIndex1 < 0);
                        var length2 = s.end - sourceIndex1;
                        if (length1 > length2)
                        {
                            length1 -= length2;
                            if (destinationIndex - sourceIndex1 > 0 && length2 > destinationIndex - sourceIndex1)
                            {
                                do
                                {
                                    s.window[destinationIndex++] = s.window[sourceIndex1++];
                                } while (--length2 != 0);
                            }
                            else
                            {
                                Array.Copy(s.window, sourceIndex1, s.window, destinationIndex, length2);
                                destinationIndex += length2;
                                num15 = sourceIndex1 + length2;
                            }
                            sourceIndex1 = 0;
                        }
                    }
                    if (destinationIndex - sourceIndex1 > 0 && length1 > destinationIndex - sourceIndex1)
                    {
                        do
                        {
                            s.window[destinationIndex++] = s.window[sourceIndex1++];
                        } while (--length1 != 0);
                        goto label_39;
                    }
                    Array.Copy(s.window, sourceIndex1, s.window, destinationIndex, length1);
                    destinationIndex += length1;
                    num15 = sourceIndex1 + length1;
                    goto label_39;
                    label_31:
                    z.Message = "invalid distance code";
                    var num24 = z.AvailableBytesIn - num2;
                    var num25 = num11 >> 3 < num24 ? num11 >> 3 : num24;
                    var num26 = num2 + num25;
                    var num27 = num1 - num25;
                    var num28 = num11 - (num25 << 3);
                    s.bitb = num10;
                    s.bitk = num28;
                    z.AvailableBytesIn = num26;
                    z.TotalBytesIn += num27 - z.NextIn;
                    z.NextIn = num27;
                    s.writeAt = destinationIndex;
                    return -3;
                    label_34:
                    num3 >>= numArray1[index1 + 1];
                    num4 -= numArray1[index1 + 1];
                    s.window[destinationIndex++] = (byte) numArray1[index1 + 2];
                    --num5;
                    goto label_39;
                    label_35:
                    if ((index2 & 32) != 0)
                    {
                        var num16 = z.AvailableBytesIn - num2;
                        var num17 = num4 >> 3 < num16 ? num4 >> 3 : num16;
                        var num18 = num2 + num17;
                        var num19 = num1 - num17;
                        var num20 = num4 - (num17 << 3);
                        s.bitb = num3;
                        s.bitk = num20;
                        z.AvailableBytesIn = num18;
                        z.TotalBytesIn += num19 - z.NextIn;
                        z.NextIn = num19;
                        s.writeAt = destinationIndex;
                        return 1;
                    }
                    z.Message = "invalid literal/length code";
                    var num29 = z.AvailableBytesIn - num2;
                    var num30 = num4 >> 3 < num29 ? num4 >> 3 : num29;
                    var num31 = num2 + num30;
                    var num32 = num1 - num30;
                    var num33 = num4 - (num30 << 3);
                    s.bitb = num3;
                    s.bitk = num33;
                    z.AvailableBytesIn = num31;
                    z.TotalBytesIn += num32 - z.NextIn;
                    z.NextIn = num32;
                    s.writeAt = destinationIndex;
                    return -3;
                    label_39:
                    ;
                }
            } while (num5 >= 258 && num2 >= 10);
            var num34 = z.AvailableBytesIn - num2;
            var num35 = num4 >> 3 < num34 ? num4 >> 3 : num34;
            var num36 = num2 + num35;
            var num37 = num1 - num35;
            var num38 = num4 - (num35 << 3);
            s.bitb = num3;
            s.bitk = num38;
            z.AvailableBytesIn = num36;
            z.TotalBytesIn += num37 - z.NextIn;
            z.NextIn = num37;
            s.writeAt = destinationIndex;
            return 0;
        }
    }
}