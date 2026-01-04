using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

public class NByte
{
	public class NBit
	{
		public Boolean Known { get; set; }
		public Boolean Value { get; set; }

		public NBit(bool val, bool know) { Value = val; Known = know; }
		public NBit(bool val) { Value = val; Known = true; }
		public NBit() { Value = false; Known = true; }

		public NBit(NBit other) { Value = other.Value; Known = other.Known; }

		public NBit(char c)
		{
			if ( c == 'X' || c == 'x' )
			{ Known = false; Value = false; return; }

			Known = true;
			if (c == '1')
			{ Value = true; return; }

			if (c == '0')
			{ Value = false; return; }

			throw new ArgumentException($"Unknown Value: '{c}'.");
		}

		public static NBit operator ^(NBit a, NBit b) => new(a.Value ^ b.Value, a.Known && b.Known);

		public override string ToString() => Known ? (Value ? "1" : "0") : "X";

		public static bool operator ==(NBit a, NBit b) => (!a.Known && !b.Known) || (a.Value == b.Value);
		public static bool operator !=(NBit a, NBit b) => (a.Known && b.Known) && (a.Value != b.Value);

        public override bool Equals(object? obj)
        {

            if (obj == null) { return false; }

            if (obj is NBit) { return this == (NBit)obj; }

            return false;
        }

        public override int GetHashCode()
        {
            return (Value ? 0b01 : 0b00) | (Known ? 0b10 : 0b00);
        }
	}

	private NBit[] Value { get; } = new NBit[8];

    public override string ToString()
    {
		string res = "";
		foreach(var bit in Value)
		{
			res += bit.ToString();
		}
		return res;
    }

	public NBit this[int key]
	{
		get => this.Value[key];
		set => this.Value[key] = value;
	}

	public void Set(byte val)
	{
		for (int i = 0; i < 8; i++)
		{
			var mask = 0b10000000 >> i;
			Value[i] = new NBit((val & mask) != 0);
		}
	}

	public NByte(byte val)
	{
		Set(val);
	}

	public NByte(NByte other)
	{
		for (int i = 0; i < 8; i++)
		{ this[i] = new(other[i]); }
	}

	public NByte() => Set(0);

	public NByte(string str)
	{
		if (str.Length != 8)
		{ throw new ArgumentException($"Incorrect length for byte: {str.Length}"); }
		for (int i = 0; i < 8; i++)
		{ Value[i] = new NBit(str[i]); }
	}

	public bool Deterministic()
	{
		foreach (var bit in Value)
		{
			if (!bit.Known)
			{  return false; }
		}
		return true;
	}

	public static implicit operator NByte(int n) => new NByte((byte)n);
	public static implicit operator int(NByte n) => (byte)n;
	public static implicit operator NByte(byte n) => new NByte(n);
	public static implicit operator byte(NByte n)
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

	public static NByte operator ^(NByte a, NByte b)
	{
		var res = new NByte();
		for (int i = 0;i < 8;i++)
		{
			res[i] = a[i] ^ b[i];
		}
		return res;
	}

	public byte[] PossibleValues()
	{
		if (Deterministic())
		{ return [(byte)this]; }
		else
		{
            List<byte> res = new();
            NByte val = new(this);

            for (int i = 0; i < 8; i++)
            {
                if (!val[i].Known)
                {
                    val[i] = new(false);
                    res.AddRange(val.PossibleValues());
                    val[i] = new(true);
                    res.AddRange(val.PossibleValues());

					return res.ToArray();
                }
            }

            return res.ToArray(); //theoretically dead code, but that's OK
        }
	}

	public void Join(byte other) => Join(new NByte(other));

	public void Join(NByte other)
	{

		for (int i = 0; i < 8; i++)
		{
			if (!this[i].Known) { continue; }

			if (!other[i].Known || other[i].Value != this[i].Value)
			{
				this[i].Known = false;
			}
		}
	}

	public NByte Lookup(NByte[] table)
	{
		var possible_indexes = PossibleValues();
		NByte[] possible_results = new NByte[possible_indexes.Length];

		for (int i = 0; i < possible_indexes.Length; i++)
		{ possible_results[i] = table[possible_indexes[i]]; }

		NByte res = new NByte(possible_results[0]);
		foreach (var result in possible_results)
		{ res.Join(result); }

		return res;
	}

	public static bool operator ==(NByte left, NByte right) => Enumerable.SequenceEqual(left.Value, right.Value);
	public static bool operator !=(NByte left, NByte right) => !Enumerable.SequenceEqual(left.Value, right.Value);

    public override bool Equals(object? obj)
    {
        if (obj == null) { return false; }

		if (obj is NByte) { return this == (NByte)obj; }

		return false;
    }

    public override int GetHashCode()
    {
		int res = 0;
        foreach (var bit in Value)
		{
			res <<= 2;
			res |= bit.GetHashCode();
		}
		return res;
    }
}
