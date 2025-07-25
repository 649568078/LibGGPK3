using System;
using System.IO;
using LibBundledGGPK3;
using LibGGPK3;
using SystemExtensions;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  GGPKTool.exe extract <ggpkPath> <internalPath> <outFile>");
            Console.WriteLine("  GGPKTool.exe replace <ggpkPath> <internalPath> <inFile>");
            return;
        }

        string command = args[0].ToLower();
        string ggpkPath = args[1];
        string internalPath = args[2];
        string filePath = args[3];

        if (!File.Exists(ggpkPath))
        {
            Console.Error.WriteLine($"Error: GGPK file not found: {ggpkPath}");
            return;
        }

        try
        {
            var ggpk = new BundledGGPK(ggpkPath, false);
            var index = ggpk.Index;
            index.ParsePaths();

            if (index.TryGetFile(internalPath, out var rec))
            {
                if (command == "extract")
                {
                    File.WriteAllBytes(filePath, rec.Read().ToArray());
                    Console.WriteLine($"Extracted to {filePath}");
                }
                else if (command == "replace")
                {
                    if (!File.Exists(filePath))
                    {
                        Console.Error.WriteLine($"Error: Input file not found: {filePath}");
                        return;
                    }
                    var data = File.ReadAllBytes(filePath);
                    rec.Write(data);
                    index.Save();
                    Console.WriteLine($"Replaced and saved.");
                }
                else
                {
                    Console.Error.WriteLine("Unknown command: " + command);
                }
            }
            else
            {
                Console.Error.WriteLine("Error: Internal path not found: " + internalPath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Exception: " + ex);
        }
    }
}