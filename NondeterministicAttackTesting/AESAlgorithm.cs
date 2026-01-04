using System;
using System.Numerics;

using bType = NByte;
public static class AESAlgorithm
{
    public static bType[][] GenRoundKeys(bType[] key)
    {
        bType[] rcon =
        { 0xFF, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1B, 0x36 };

        int N = key.Length / 4;

        bType[][] K = new bType[N][];
        for (int i = 0; i < N; i++)
        { K[i] = [ key[i*4+0], key[i*4+1], key[i*4+2], key[i*4+3] ]; }

        int R = N switch
        {
            4 => 11, // N = 4 -> AES128
            6 => 13, // N = 6 -> AES192
            8 => 15, // N = 8 -> AES256
            _ => throw new ArgumentException("Key of unknown size"),
        };

        bType[] W = new bType[R*4*4];

        var GetWord = (int i) => new bType[4]{ W[i*4+0],  W[i*4+1], W[i*4+2], W[i*4+3] };

        var SetWord = (int i, bType[] word) =>
        {
            if (word.Length != 4)
            { throw new ArgumentException("Word of length other than 4"); }
            W[i*4+0] = word[0];
            W[i*4+1] = word[1];
            W[i*4+2] = word[2];
            W[i*4+3] = word[3];
        };

        var RotWord = (int i) => new bType[4] { W[i*4+1], W[i*4+2], W[i*4+3], W[i*4+0] };

        var SubWord = (bType[] w) =>
        {
            var res = new bType[4];
            res[0] = SBox(w[0]);
            res[1] = SBox(w[1]);
            res[2] = SBox(w[2]);
            res[3] = SBox(w[3]);
            return res;
        };

        var XorWord = (bType[] a, bType[] b) =>
        {
            var res = new bType[4];
            res[0] = (bType)(a[0] ^ b[0]);
            res[1] = (bType)(a[1] ^ b[1]);
            res[2] = (bType)(a[2] ^ b[2]);
            res[3] = (bType)(a[3] ^ b[3]);
            return res;
        };

        var XorRCon = (bType[] w, int i) =>
        {
            var res = new bType[4];
            res[0] = (bType)(w[0] ^ rcon[i]);
            res[1] = w[1];
            res[2] = w[2];
            res[3] = w[3];
            return res;
        };

        for (int i = 0; i < 4*R; i++)
        {
            if (i < N)
            { SetWord(i, K[i]); }
            else if (i >= N && (i%N)==0)
            { SetWord(i, XorRCon(XorWord(GetWord(i - N), SubWord(RotWord(i - 1))), i/N));  }
            else if (i >= N && N > 6 && (i%N)==4)
            { SetWord(i, XorWord(GetWord(i - N), SubWord(GetWord(i - 1))));  }
            else
            { SetWord(i, XorWord(GetWord(i - N), GetWord(i - 1))); }
        }

        bType[][] res = new bType[R][];
        for( int i = 0; i < R; i++ )
        {
            res[i] = new bType[16];
            for (int j = 0; j < 16; j++)
            { res[i][j] = W[i * 16 + j]; }
        }
        return res;
    }

    public static bType SBox(bType holder, bool inverse = false)
    {
        bType[] sbox =
        [
            0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76,
            0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0,
            0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15,
            0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75,
            0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84,
            0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf,
            0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8,
            0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2,
            0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73,
            0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb,
            0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79,
            0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08,
            0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a,
            0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e,
            0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf,
            0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16
        ];
        bType[] invsbox =
        [
            0x52, 0x09, 0x6a, 0xd5, 0x30, 0x36, 0xa5, 0x38, 0xbf, 0x40, 0xa3, 0x9e, 0x81, 0xf3, 0xd7, 0xfb,
            0x7c, 0xe3, 0x39, 0x82, 0x9b, 0x2f, 0xff, 0x87, 0x34, 0x8e, 0x43, 0x44, 0xc4, 0xde, 0xe9, 0xcb,
            0x54, 0x7b, 0x94, 0x32, 0xa6, 0xc2, 0x23, 0x3d, 0xee, 0x4c, 0x95, 0x0b, 0x42, 0xfa, 0xc3, 0x4e,
            0x08, 0x2e, 0xa1, 0x66, 0x28, 0xd9, 0x24, 0xb2, 0x76, 0x5b, 0xa2, 0x49, 0x6d, 0x8b, 0xd1, 0x25,
            0x72, 0xf8, 0xf6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xd4, 0xa4, 0x5c, 0xcc, 0x5d, 0x65, 0xb6, 0x92,
            0x6c, 0x70, 0x48, 0x50, 0xfd, 0xed, 0xb9, 0xda, 0x5e, 0x15, 0x46, 0x57, 0xa7, 0x8d, 0x9d, 0x84,
            0x90, 0xd8, 0xab, 0x00, 0x8c, 0xbc, 0xd3, 0x0a, 0xf7, 0xe4, 0x58, 0x05, 0xb8, 0xb3, 0x45, 0x06,
            0xd0, 0x2c, 0x1e, 0x8f, 0xca, 0x3f, 0x0f, 0x02, 0xc1, 0xaf, 0xbd, 0x03, 0x01, 0x13, 0x8a, 0x6b,
            0x3a, 0x91, 0x11, 0x41, 0x4f, 0x67, 0xdc, 0xea, 0x97, 0xf2, 0xcf, 0xce, 0xf0, 0xb4, 0xe6, 0x73,
            0x96, 0xac, 0x74, 0x22, 0xe7, 0xad, 0x35, 0x85, 0xe2, 0xf9, 0x37, 0xe8, 0x1c, 0x75, 0xdf, 0x6e,
            0x47, 0xf1, 0x1a, 0x71, 0x1d, 0x29, 0xc5, 0x89, 0x6f, 0xb7, 0x62, 0x0e, 0xaa, 0x18, 0xbe, 0x1b,
            0xfc, 0x56, 0x3e, 0x4b, 0xc6, 0xd2, 0x79, 0x20, 0x9a, 0xdb, 0xc0, 0xfe, 0x78, 0xcd, 0x5a, 0xf4,
            0x1f, 0xdd, 0xa8, 0x33, 0x88, 0x07, 0xc7, 0x31, 0xb1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xec, 0x5f,
            0x60, 0x51, 0x7f, 0xa9, 0x19, 0xb5, 0x4a, 0x0d, 0x2d, 0xe5, 0x7a, 0x9f, 0x93, 0xc9, 0x9c, 0xef,
            0xa0, 0xe0, 0x3b, 0x4d, 0xae, 0x2a, 0xf5, 0xb0, 0xc8, 0xeb, 0xbb, 0x3c, 0x83, 0x53, 0x99, 0x61,
            0x17, 0x2b, 0x04, 0x7e, 0xba, 0x77, 0xd6, 0x26, 0xe1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0c, 0x7d
        ];

        var box = inverse ? invsbox : sbox;

        Func<bType> subByte;

        if (IsBuiltinNumber())
        { subByte = () => box[holder]; }
        else if(typeof(bType) == typeof(NByte))
        { subByte = () => holder.Lookup(box); }
        else
        { throw new NotImplementedException(); }

        return subByte();
    }

    // Mostly Copied from Wikipedia
    // Galois Field (256) Multiplication of two bytes
    public static byte GMulByte(byte a, byte b)
    {
            byte p = 0;

            for (int counter = 0; counter < 8; counter++)
            {
                if ((b & 1) != 0)
                {
                    p ^= a;
                }

                bool overflow = (a & 0x80) != 0;
                a <<= 1;
                if (overflow)
                { a ^= 0x1B; /* x^8 + x^4 + x^3 + x + 1 */ }

                b >>= 1;
            }

            return p;
    }

    public static byte[][]? GMulByteTable = null;

    public static void FillGMulTable()
    {
        GMulByteTable = new byte[256][];
        for (int a = 0; a < 256; a++)
        {
            GMulByteTable[a] = new byte[256];
            for(int b = 0; b < 256; b++)
            {
                GMulByteTable[a][b] = GMulByte((byte)a, (byte)b);
            }
        }
    }

    public static byte GMulTable(byte a, byte b) => GMulByteTable![a][b];

    public static Dictionary<int, bType> GMulCache = new();
    public static bType GMul(bType a, bType b)
    {
        a = new(a); b = new(b);
        try
        {
            int lookupKey = (a.GetHashCode() << 16) | b.GetHashCode();
            NByte tmpA = new(a);
            NByte tmpB = new(b);
            bType? fromCache;
            if (!GMulCache.TryGetValue(lookupKey, out fromCache))
            {
                /* Interestingly, both methods take about the same amount of time
                bType p = 0;

                for (int counter = 0; counter < 8; counter++)
                {
                    if (b[7].Known)
                    {
                        if (b[7].Value)
                        { p ^= a; }
                    }
                    else
                    { p.Join(p ^ a); }

                    bool overflow = a[0].Value;
                    bool overflowKnown = a[0].Known;

                    // a <<= 1
                    for (int i = 0; i < 7; i++)
                    { a[i] = new(a[i + 1]); }
                    a[7] = new(val: false, know: true);
                    // end a <<= 1

                    if (overflowKnown)
                    {
                        if (overflow)
                        { a ^= 0x1B; } // x^8 + x^4 + x^3 + x + 1
                    }
                    else
                    { a.Join(a ^ 0x1B ); } // x^8 + x^4 + x^3 + x + 1

                    // b >>= 1
                    for (int i = 7; i > 0; i--)
                    { b[i] = new(b[i - 1]); }
                    b[0] = new();
                    // end b >>= 1
                    continue;
                }

                GMulCache[lookupKey] = new(p);
                return p;
                /*/ 
                bType? res = null;
                foreach (byte p_a in a.PossibleValues())
                {
                    foreach (byte p_b in b.PossibleValues())
                    {
                        if (res is null)
                        {
                            res = GMulTable(p_a, p_b);
                        }
                        else
                        {
                            res.Join(GMulTable(p_a, p_b));
                        }
                    }
                }
                GMulCache[lookupKey] = new(res!);
                return res!;
                //*/
            }
            return fromCache!;
        }
        catch (NullReferenceException)
        {
            FillGMulTable();
            return GMul(a, b);
        }
        
        throw new NotImplementedException($"GMul Not Implemented for data type of {typeof(bType)}");
    }

    // Mostly Copied from Wikipedia
    // 's' is the main State matrix, 'ss' is a temp matrix of the same dimensions as 's'.
    public static bType[] MixColumns(bType[] state)
    {
        var res = new bType[state.Length];

        for (int c = 0; c < state.Length/4; c++)
        {
            res[4 * c + 0] = (bType)(GMul(2, state[4 * c + 0]) ^ GMul(3, state[4 * c + 1]) ^ GMul(1, state[4 * c + 2]) ^ GMul(1, state[4 * c + 3]));
            res[4 * c + 1] = (bType)(GMul(1, state[4 * c + 0]) ^ GMul(2, state[4 * c + 1]) ^ GMul(3, state[4 * c + 2]) ^ GMul(1, state[4 * c + 3]));
            res[4 * c + 2] = (bType)(GMul(1, state[4 * c + 0]) ^ GMul(1, state[4 * c + 1]) ^ GMul(2, state[4 * c + 2]) ^ GMul(3, state[4 * c + 3]));
            res[4 * c + 3] = (bType)(GMul(3, state[4 * c + 0]) ^ GMul(1, state[4 * c + 1]) ^ GMul(1, state[4 * c + 2]) ^ GMul(2, state[4 * c + 3]));
        }

        return res;
    }

    // Mostly Copied from non-inverse
    public static bType[] InvMixColumns(bType[] state)
    {
        var res = new bType[state.Length];

        for (int c = 0; c < state.Length/4; c++)
        {
            res[4 * c + 0] = (byte)(GMul(14, state[4 * c + 0]) ^ GMul(11, state[4 * c + 1]) ^ GMul(13, state[4 * c + 2]) ^ GMul(09, state[4 * c + 3]));
            res[4 * c + 1] = (byte)(GMul(09, state[4 * c + 0]) ^ GMul(14, state[4 * c + 1]) ^ GMul(11, state[4 * c + 2]) ^ GMul(13, state[4 * c + 3]));
            res[4 * c + 2] = (byte)(GMul(13, state[4 * c + 0]) ^ GMul(09, state[4 * c + 1]) ^ GMul(14, state[4 * c + 2]) ^ GMul(11, state[4 * c + 3]));
            res[4 * c + 3] = (byte)(GMul(11, state[4 * c + 0]) ^ GMul(13, state[4 * c + 1]) ^ GMul(09, state[4 * c + 2]) ^ GMul(14, state[4 * c + 3]));
        }

        return res;
    }

    public static bool IsBuiltinNumber() => typeof(bType) == typeof(byte) || typeof(bType) == typeof(Byte) || typeof(bType) == typeof(int);

    public static bType[] SBox(bType[] state)
    {
        var res = new bType[state.Length];
        for (int i = 0; i < state.Length; i++)
        { res[i] = SBox(state[i]); }
        return res;
    }

    public static bType[] InvSBox(bType[] state)
    {
        var res = new bType[state.Length];
        for (int i = 0; i < state.Length; i++)
        { res[i] = SBox(state[i], inverse: true); }
        return res;
    }

    public static bType[] ShiftRows(bType[] state)
    {
        var res = new bType[state.Length];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            { res[i * 4 + j] = state[((j + i) % 4) * 4 + j]; }
        }
        return res;
    }

    public static bType[] InvShiftRows(bType[] state)
    {
        var res = new bType[state.Length];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            { res[((j + i) % 4) * 4 + j] = state[i * 4 + j]; }
        }
        return res;
    }

    public static bType[] AddRoundKey(bType[] state, bType[] roundKey)
    {
        var res = new bType[state.Length];
        for (int i = 0; i < state.Length; i++)
        { res[i] = (bType)(state[i] ^ roundKey[i]); }
        return res;
    }

    public static bType[] Encrypt(bType[] plaintext, bType[] key)
    {
        var roundKeys = GenRoundKeys(key);
        int r = 0;

        // initial round
        //*
        var state = AddRoundKey(plaintext, roundKeys[r]);
        /*/
        var state = new bType[plaintext.Length];
        plaintext.CopyTo(state, 0);
        //*/
        //Console.WriteLine(MainClass.MakeString(state));

        for (r = 1; r < roundKeys.Length-1; r++)
        {
            state = SBox(state);
            //Console.WriteLine(MainClass.MakeString(state));
            state = ShiftRows(state);
            //Console.WriteLine(MainClass.MakeString(state));
            state = MixColumns(state);
            //Console.WriteLine(MainClass.MakeString(state));
            state = AddRoundKey(state, roundKeys[r]);
            //Console.WriteLine(MainClass.MakeString(state));
            //Console.ReadLine();
        }

        // final round
        state = SBox(state);
        state = ShiftRows(state);
        state = AddRoundKey(state, roundKeys[r]);

        return state;
    }

    public static bType[] Decrypt(bType[] ciphertext, bType[] key)
    {
        var roundKeys = GenRoundKeys(key);
        
        int r = roundKeys.Length - 1;

        // final round
        var state = AddRoundKey(ciphertext, roundKeys[r]);
        state = InvShiftRows(state);
        state = InvSBox(state);

        for (r -= 1; r > 0; r--)
        {
            state = AddRoundKey(state, roundKeys[r]);
            state = InvMixColumns(state);
            state = InvShiftRows(state);
            state = InvSBox(state);
        }

        // initial round
        state = AddRoundKey(state, roundKeys[0]);

        return state;
    }
}
