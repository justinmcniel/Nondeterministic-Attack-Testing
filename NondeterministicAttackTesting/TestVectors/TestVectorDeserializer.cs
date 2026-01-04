using System;

namespace TestVectors;
public static class Deserializer
{
    public static List<AESTestVector> Deserialize(string path, int keySize, AESTestVector.SpecialModes specialMode)
    {
        string fileName = Path.GetFileName(path);
        UInt64 lineCount = 0;
        AESTestVector.OperationModes? currentMode = null;
        string? currentVectorID = null;
        string? currentKey = null;
        string? currentPlaintext = null;
        string? currentCiphertext = null;
        string? currentIV = null;
        List<String>? currentIntermediateValues = null;
        UInt64 testVectorCount = 0;

        List<AESTestVector> res = new List<AESTestVector>();

        var currentVectorReady = () => { return currentKey != null && currentPlaintext != null && currentCiphertext != null && currentMode != null; };
        var ClearCurrentVector = () =>
        {
            currentVectorID = null;
            currentKey = null;
            currentPlaintext = null;
            currentCiphertext = null;
            currentIntermediateValues = null;
            currentIV = null;
        };
        var AddCurrentVector = () =>
        {
            if (currentVectorReady())
            {
                string source = $"{fileName}: {currentVectorID}";
                res.Add(new AESTestVector(source, (AESTestVector.OperationModes)currentMode!, currentKey!, currentPlaintext!, currentCiphertext!, keySize, specialMode, currentIntermediateValues, currentIV));
                testVectorCount++;
                ClearCurrentVector();
            }
            else
            {
                throw new Exception($"Vector Not Complete: mode: {currentMode} k: {currentKey}; pt: {currentPlaintext}; ct: {currentCiphertext}");
            }
        };

        using (var reader = new StreamReader(path))
        {
            string? line;
            while ((line = reader.ReadLine()?.Trim().TrimEnd('.')) != null)
            {
                lineCount++;
                if (line.StartsWith("COUNT")) // start new vector
                {
                    if (currentVectorReady())
                    { AddCurrentVector(); }
                    currentVectorID = line.Substring("COUNT = ".Length);
                }
                else if (line.StartsWith("KEY = ")) { currentKey = line.Substring("KEY = ".Length); }
                else if (line.StartsWith("PLAINTEXT = ")) { currentPlaintext = line.Substring("PLAINTEXT = ".Length); }
                else if (line.StartsWith("CIPHERTEXT = ")) { currentCiphertext = line.Substring("CIPHERTEXT = ".Length); }
                else if (line.StartsWith("INTERMEDIATE COUNT = "))
                { 
                    currentIntermediateValues ??= new();
                    int expectedCount = int.Parse(line.Substring("INTERMEDIATE COUNT = ".Length));
                    if (expectedCount != currentIntermediateValues.Count)
                    {
                        Console.WriteLine($"Expected {expectedCount} intermediate values, but got {currentIntermediateValues.Count} ({fileName}: {lineCount})");
                    }
                }
                else if (line.StartsWith("Intermediate Vaue ")) // Nice Typo NIST
                {
                    currentIntermediateValues ??= new(); // handle the error case where it hasn't been set up yet
                    if (line.StartsWith("Intermediate Vaue CIPHERTEXT = ")) { currentIntermediateValues.Add(line.Substring("Intermediate Vaue CIPHERTEXT = ".Length));  }
                    else if (line.StartsWith("Intermediate Vaue PLAINTEXT = ")) { currentIntermediateValues.Add(line.Substring("Intermediate Vaue PLAINTEXT = ".Length));  }
                }
                else if (line.StartsWith("IV = ")) { currentIV = line.Substring("IV = ".Length); }
                else if (line.Length == 0) { } // empty line, ignore
                else if (line.StartsWith('#')) { } // comment, ignore
                else if (line == "[ENCRYPT]") { currentMode = AESTestVector.OperationModes.Encrypt; }
                else if (line == "[DECRYPT]") { currentMode = AESTestVector.OperationModes.Decrypt; }
                else
                {
                    Console.WriteLine($"Unhandled Line ({fileName}:{lineCount}): {line}");
                }
            }
        }

        AddCurrentVector();

        return res;
    }
}
