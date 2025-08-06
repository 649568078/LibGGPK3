using System;
using System.IO;
using LibBundledGGPK3;
using LibBundle3.Nodes;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 4 && !(args.Length == 3 && args[0].ToLower() == "extract-dir"))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  GGPKTool.exe extract <ggpkPath> <internalPath> <outFile>");
            Console.WriteLine("  GGPKTool.exe replace <ggpkPath> <internalPath> <inFile>");
            Console.WriteLine("  GGPKTool.exe extract-dir <ggpkPath> <internalDir> <outDir>");
            return;
        }

        string command = args[0].ToLower();
        string ggpkPath = args[1];
        string internalPath = args[2];
        string filePath = args.Length > 3 ? args[3] : null;

        if (!File.Exists(ggpkPath))
        {
            Console.Error.WriteLine($"Error: GGPK file not found: {ggpkPath}");
            return;
        }

        try
        {
            using var ggpk = new BundledGGPK(ggpkPath, false);
            var index = ggpk.Index;
            index.ParsePaths();

            if (command == "extract")
            {
                if (index.TryGetFile(internalPath, out var rec))
                {
                    File.WriteAllBytes(filePath, rec.Read().ToArray());
                    Console.WriteLine($"Extracted to {filePath}");
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                }
            }
            else if (command == "replace")
            {
                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine($"Error: Input file not found: {filePath}");
                    return;
                }
                if (index.TryGetFile(internalPath, out var rec))
                {
                    var data = File.ReadAllBytes(filePath);
                    rec.Write(data);
                    index.Save();
                    Console.WriteLine($"Replaced and saved.");
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                }
            }
            else if (command == "extract-dir")
            {
                // 目录提取
                var nodePath = internalPath.TrimEnd('/');
                if (!index.TryFindNode(nodePath, out var node, null))
                {
                    Console.Error.WriteLine("Error: Internal directory not found: " + nodePath);
                    return;
                }

                Console.WriteLine($"Extracting directory: {nodePath}");
                int count = LibBundle3.Index.ExtractParallel(node, filePath, (fr, path) =>
                {
                    Console.WriteLine("Extracted: " + path);
                    return false; // 不跳过任何文件
                });

                Console.WriteLine($"✅ Done! Extracted {count} files.");
            }
            else
            {
                Console.Error.WriteLine("Unknown command: " + command);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Exception: " + ex);
        }
    }
}