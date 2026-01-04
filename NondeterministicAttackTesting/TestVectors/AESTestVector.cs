using bType = NByte;

namespace TestVectors
{
	public class AESTestVector
    {
#pragma warning disable CS8618
        public AESTestVector(String source, OperationModes mode, string key, string plaintext, string ciphertext, int keySize, SpecialModes specialMode, List<string>? intermediate_values, string? iv)
		{
			Source = source;
			OperationMode = mode;
			KeyStr = key;
			PlaintextStr = plaintext;
			CiphertextStr = ciphertext;
			KeySize = keySize;
			SpecialMode = SpecialMode;
			IntermediateValuesStr = intermediate_values ?? new();
			if (iv != null) { IVStr = iv; }
			if (Key!.Length != KeySize/8)
			{
                Console.WriteLine($"Expected Key of Length: {KeySize} got {Key.Length * 8}");
                Console.WriteLine(this);

                throw new InvalidDataException($"Expected Key of Length: {KeySize} got {Key.Length * 8}");
			}
        }
#pragma warning restore CS8618

        public String Source { get; protected set; }
		public enum OperationModes
		{
			Encrypt,
			Decrypt,
		}

		public enum SpecialModes
		{
			ECB,
		}

		public static List<int> KeySizes = new() { 128, 192, 256 };

		public OperationModes OperationMode { get; protected set; }

        public bType[] Key { get; protected set; }
        public bType[] Plaintext { get; protected set; }
        public bType[] Ciphertext { get; protected set; }
		public bType[]? IV { get; protected set; }
		public int KeySize { get; protected set; }
		public SpecialModes SpecialMode { get; protected set; }

        public List<bType[]> IntermediateValues;

		public string KeyStr
		{
			get => MainClass.MakeString(Key);
			set => Key = MainClass.FromString(value);
        }

        public string PlaintextStr
        {
            get => MainClass.MakeString(Plaintext);
            set => Plaintext = MainClass.FromString(value);
        }

        public string CiphertextStr
        {
            get => MainClass.MakeString(Ciphertext);
            set => Ciphertext = MainClass.FromString(value);
        }

        public string IVStr
        {
			get
			{
				if (IV == null)
				{
					return "null";
				}
				return MainClass.MakeString(IV);
			}
            set => IV = MainClass.FromString(value);
        }

        public List<string> IntermediateValuesStr
		{
			get => (from IntermediateValue in IntermediateValues select MainClass.MakeString(IntermediateValue)).ToList();
			set => IntermediateValues = (from IntermediateValue in value select MainClass.FromString(IntermediateValue)).ToList();
		}

        public override String ToString()
		{
			var res = $"Source: {Source}; Operation Mode: {OperationMode}; Key: {KeyStr}; Plaintext: {PlaintextStr}; CipherText: {CiphertextStr}; Special Mode: {SpecialMode}; Key Size: {KeySize}; ";
			if (IntermediateValues.Count > 0)
			{
				res += "IntermediateValues: ";
				foreach (var value in IntermediateValuesStr)
				{
					res += $"{value}; ";
				}
			}
			return res;
		}
    }
}