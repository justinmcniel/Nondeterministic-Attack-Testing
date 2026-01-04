using System;
using System.Linq;
using System.Security.AccessControl;

using FType = float;
public class PByte
{
	public class PBit
	{
		public FType Probability { get; set; }
		public Boolean Value { get => (Probability >= 0.5); }
		public Boolean Known { get => ((Probability == 0.0) || (Probability == 1.0)); }

		public PBit(bool val) => Probability = (FType)(val ? 1.0 : 0.0);
		public PBit(FType prob) => Probability = prob;
		public PBit() => Probability = (FType)0.0;
		public PBit(char c)
        {
            if (c == 'X' || c == 'x')
            { Probability = (FType)0.5; return; }

            if (c == '1')
            { Probability = (FType)1.0; return; }

            if (c == '0')
            { Probability = (FType)0.0; return; }

            throw new ArgumentException($"Unknown Value: '{c}'.");
        }

		public PBit(string s) => Convert.ToDouble(s);

		public static PBit operator ^(PBit a, PBit b) =>  new(a.Probability*(1-b.Probability) + (1-a.Probability)*b.Probability);

		public override string ToString() => Probability.ToString(".##");

		public static FType operator ==(PBit a, PBit b) => a.Probability*b.Probability + (1-a.Probability)*(1-b.Probability);
		public static FType operator !=(PBit a, PBit b) => (a ^ b).Probability;

        public override bool Equals(object? obj)
        {

            if (obj == null) { return false; }

            if (obj is PBit) { return (this == (PBit)obj) >= (FType)0.5; }

            return false;
        }

        public override int GetHashCode() => (int)(Probability*8);

        public static implicit operator PBit(FType p) => new(p);
        public static implicit operator FType(PBit b) => b.Probability;
    }

    public PBit[] Value { get; } = new PBit[8];

    public override string ToString()
    {
        string res = "";
        foreach (var bit in Value)
        {
            res += " ";
            res += bit.ToString();
        }
        return res.Substring(1);
    }

    public PBit this[int key]
    {
        get => this.Value[key];
        set => this.Value[key] = value;
    }

    public void Set(byte val)
    {
        for (int i = 0; i < 8; i++)
        {
            var mask = 0b10000000 >> i;
            Value[i] = new PBit((val & mask) != 0);
        }
    }

    public PByte(byte val)
    {
        Set(val);
    }

    public PByte(PByte other)
    {
        for (int i = 0; i < 8; i++)
        { this[i] = new(other[i].Probability); }
    }

    public PByte() => Set(0);

    public PByte(string str)
    {
        char[] delimeters = [' ', '\t', '\n', ',', '.', ';', ':'];
        bool containsDelimeter = false;
        foreach (var c in delimeters) { if (str.Contains(c)) { containsDelimeter = true; break; } }
        if (containsDelimeter)
        {
            var splits = str.Split(delimeters);
            if (splits.Length != 8)
            {  throw new ArgumentException($"Incorrect length for byte: {splits.Length}"); }
            for (int i = 0; i < 8; i++)
            { Value[i] = new PBit(splits[i]); }
            return;
        }
        else
        {
            for (int i = 0; i < 8; i++)
            { Value[i] = new PBit(str[i]); }
        }
    }

    public bool Deterministic()
    {
        foreach (var bit in Value)
        {
            if (!bit.Known)
            { return false; }
        }
        return true;
    }

    public static implicit operator PByte(int n) => new PByte((byte)n);
    public static implicit operator PByte(byte n) => new PByte(n);
    public static implicit operator int(PByte n) => (byte)n;
    public static implicit operator byte(PByte n)
    {
        if (!n.Deterministic())
        { throw new NotFiniteNumberException(); } // not ideal, but easier than making a new exception

        byte val = 0;
        for (int i = 0; i < 8; i++)
        {
            byte mask = (byte)(0b10000000 >> i);

            if (n[i].Value)
            { val |= mask; }
        }
        return val;
    }

    public static PByte operator ^(PByte a, PByte b)
    {
        var res = new PByte();
        for (int i = 0; i < 8; i++)
        {
            res[i] = a[i] ^ b[i];
        }
        return res;
    }

    public struct PValue
    {
        public byte value;
        public FType probability;
    }

    public PValue[] PossibleValues(FType prob = (FType)1.0)
    {
        if (Deterministic())
        { return [new PValue { probability = prob, value = (byte)this }]; }
        else
        {
            List<PValue> res = new();
            PByte val = new(this);

            for (int i = 0; i < 8; i++)
            {
                if (!val[i].Known)
                {
                    val[i] = new(false);
                    res.AddRange(val.PossibleValues(prob * (1 - this[i].Probability)));
                    val[i] = new(true);
                    res.AddRange(val.PossibleValues(prob * this[i].Probability));

                    return res.ToArray();
                }
            }

            return res.ToArray(); //theoretically dead code, but that's OK
        }
    }

    public static PByte Join(PValue[] lst)
    {
        PByte res = new();
        foreach (var val in lst)
        {
            for (int i = 0; i < 8; i++)
            {
                byte mask = (byte)(0b10000000 >> i);
                if ((val.value & mask) != 0)
                { res[i].Probability += val.probability; }
            }
        }
        return res;
    }

    public static PByte Join(PByte a, FType aProb, PByte b, FType bProb)
    {
        PByte res = new();
        for (int i = 0; i < 8; i++)
        { res[i].Probability = (a[i].Probability * aProb) + (b[i].Probability * bProb); }
        return res;
    }

    public PByte Lookup(PByte[] table)
    {
        var possible_indexes = PossibleValues();
        PValue[] possible_results = new PValue[possible_indexes.Length];

        for (int i = 0; i < possible_indexes.Length; i++)
        {
            possible_results[i] = new PValue 
            { 
                probability = possible_indexes[i].probability, 
                value = table[possible_indexes[i].value] 
            };
        }

        return Join(possible_results);
    }

    public static FType operator ==(PByte a, PByte b)
    {
        FType res = (FType)1.0;
        for (int i = 0; i < 8; i++)
        {
            res *= (a[i].Probability * b[i].Probability) + ((1 - a[i].Probability) * (1 - b[i].Probability));
        }
        return res;
    }
    public static FType operator !=(PByte a, PByte b)
    {
        FType res = (FType)1.0;
        for (int i = 0; i < 8; i++)
        {
            res *= (a[i].Probability * (1 - b[i].Probability)) + ((1 - a[i].Probability) * b[i].Probability);
        }
        return res;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) { return false; }

        if (obj is PByte) { return (this == (PByte)obj) >= (FType)0.5; }

        return false;
    }

    public override int GetHashCode()
    {
        int res = 0;
        foreach (var bit in Value)
        {
            res <<= 3;
            res |= bit.GetHashCode();
        }
        return res;
    }
}
