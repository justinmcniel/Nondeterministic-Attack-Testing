using System.Diagnostics;
using bType = NByte;

class MainClass
{
    const bool verify = false;

    public static string MakeString(bType[] data)
    {
        if (typeof(bType) == typeof(byte))
        { return Convert.ToHexString(Array.ConvertAll(data, item => (byte)item)); }
        string res = "";
        foreach (var item in data)
        {
            res += $" {item.ToString()}";
        }
        return res.Substring(1);
    }

    public static bType[] FromString(string str)
    {
        try
        {
            return Array.ConvertAll(Convert.FromHexString(str), item => (bType)item);
        }
        catch
        {
            List<bType> res = new();
            foreach (var item in str.Split([ ' ', ',', ';' ]))
            {
                res.Add(new bType(item));
            }
            return res.ToArray();
        }
    }

    static void Main(string[] args)
    {
        string basePath = "..\\..\\..\\TestVectorsData\\AES";

        if (!Directory.Exists(basePath))
        {
            Console.WriteLine("Base Path for test vectors does not exist");
            return;
        }

        List<TestVectors.AESTestVector.SpecialModes> specialModes = new() 
        {
            TestVectors.AESTestVector.SpecialModes.ECB,
        };
        Dictionary<int, List<TestVectors.AESTestVector>> testVectors = new();

        foreach (var specialMode in specialModes)
        {
            foreach (var keySize in TestVectors.AESTestVector.KeySizes)
            {
                List<TestVectors.AESTestVector> tmp = new();

                foreach (string fname in Directory.EnumerateFiles(basePath, $"{specialMode}*{keySize}*", SearchOption.AllDirectories))
                {
                    try
                    {
                        tmp.AddRange(TestVectors.Deserializer.Deserialize(fname, keySize, specialMode));
                    }
                    catch
                    {
                        Console.WriteLine($"Failed: {fname}");
                    }
                }
                testVectors[keySize] = tmp;
            }
        }

        AESAlgorithm.FillGMulTable();

        if (verify)
        {
            UInt64 testedVectors = 0;
            UInt64 skippedVectors = 0;
            UInt64 failures = 0;

            Console.WriteLine("Verifying AES Implementation");

            foreach (var keySize in TestVectors.AESTestVector.KeySizes)
            {
                foreach (var testVector in testVectors[keySize])
                {
                    if (testVector.IntermediateValues.Count > 0 || testVector.IV != null)
                    { Interlocked.Increment(ref skippedVectors); continue; }

                    Interlocked.Increment(ref testedVectors);
                    switch (testVector.OperationMode)
                    {
                        case TestVectors.AESTestVector.OperationModes.Encrypt:
                            var testCT = AESAlgorithm.Encrypt(testVector.Plaintext, testVector.Key);
                            bool failed = false;

                            if (!Enumerable.SequenceEqual(testCT, testVector.Ciphertext))
                            {
                                Console.WriteLine($"Failed to get desired encryption");
                                Console.WriteLine($"Got: {MakeString(testCT)}");
                                failed = true;
                            }

                            var testPT = AESAlgorithm.Decrypt(testCT, testVector.Key);
                            if (!Enumerable.SequenceEqual(testPT, testVector.Plaintext))
                            {
                                Console.WriteLine($"Failed to decrypt");
                                Console.WriteLine($"Got: {MakeString(testPT)}");
                                failed = true;
                            }

                            if (failed) { Console.WriteLine(testVector); Interlocked.Increment(ref failures); _ = Console.ReadLine(); }
                            break;
                        case TestVectors.AESTestVector.OperationModes.Decrypt:
                            var testPT2 = AESAlgorithm.Decrypt(testVector.Ciphertext, testVector.Key);
                            bool failed2 = false;

                            if (!Enumerable.SequenceEqual(testPT2, testVector.Plaintext))
                            {
                                Console.WriteLine($"Failed to get desired decryption");
                                Console.WriteLine($"Got: {MakeString(testPT2)}");
                                failed2 = true;
                            }

                            var testCT2 = AESAlgorithm.Encrypt(testPT2, testVector.Key);
                            if (!Enumerable.SequenceEqual(testCT2, testVector.Ciphertext))
                            {
                                Console.WriteLine($"Failed to encrypt");
                                Console.WriteLine($"Got: {MakeString(testCT2)}");
                                failed2 = true;
                            }

                            if (failed2) { Console.WriteLine(testVector); Interlocked.Increment(ref failures); _ = Console.ReadLine(); }
                            break;
                        default: throw new NotImplementedException();
                    }

                    if (testedVectors % 100 == 0)
                    {
                        Console.WriteLine($"Tested: {testedVectors}");
                        Console.WriteLine($"Skipped: {skippedVectors}");
                        Console.WriteLine($"Failed: {failures}");
                    }
                }
            }

            Console.WriteLine($"Tested: {testedVectors}");
            Console.WriteLine($"Skipped: {skippedVectors}");
            Console.WriteLine($"Failed: {failures}");
        }
        var vector = testVectors[128][0];
        //Console.WriteLine(vector.PlaintextStr);
        Console.WriteLine("CipherTexts: (OG, From Modified PT)");
        Console.WriteLine(vector.CiphertextStr);

        vector.Plaintext[15][7].Known = false;

        //Console.WriteLine(vector.PlaintextStr);

        Stopwatch sw = Stopwatch.StartNew();
        var ct = AESAlgorithm.Encrypt(vector.Plaintext, vector.Key);
        sw.Stop();

        Console.WriteLine(MakeString(ct));

        Console.WriteLine("\nModified PT: ");
        Console.WriteLine(vector.PlaintextStr);

        Console.WriteLine(sw.ElapsedMilliseconds);
    }
}