using System;
using System.IO;
using System.IO.Compression;
using LibBundledGGPK3;
using LibBundle3;
using LibBundle3.Nodes;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  GGPKTool.exe extract <ggpkOrIndexPath> <internalPath> <outFile>");
            Console.WriteLine("  GGPKTool.exe replace <ggpkOrIndexPath> <internalPath> <inFile>");
            Console.WriteLine("  GGPKTool.exe replace-zip <ggpkOrIndexPath> <zipFile>");
            Console.WriteLine("  GGPKTool.exe extract-dir <ggpkOrIndexPath> <internalDir> <outDir>");
            return 1;
        }

        string command = args[0].ToLower();
        string inputPath = args[1];
        string internalPath = args.Length > 2 ? args[2] : null;
        string filePath = args.Length > 3 ? args[3] : null;

        try
        {
            bool isSteam = Path.GetFileName(inputPath).Equals("_.index.bin", StringComparison.OrdinalIgnoreCase);

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Error: File not found: {inputPath}");
                return 1;
            }

            if (isSteam)
            {
                // --- Steam Bundles2 模式 ---
                using var index = new LibBundle3.Index(inputPath, false);
                index.ParsePaths();

                return RunCommand(index, command, internalPath, filePath);
            }
            else
            {
                // --- Standalone GGPK 模式 ---
                using var ggpk = new BundledGGPK(inputPath, false);
                var index = ggpk.Index;
                index.ParsePaths();

                return RunCommand(index, command, internalPath, filePath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Exception: " + ex);
            return 1;
        }
    }

    static int RunCommand(LibBundle3.Index index, string command, string? internalPath, string? filePath)
    {
        switch (command)
        {
            case "extract":
                if (internalPath == null || filePath == null)
                {
                    Console.Error.WriteLine("Usage: extract <inputPath> <internalPath> <outFile>");
                    return 1;
                }
                if (index.TryGetFile(internalPath, out var rec))
                {
                    File.WriteAllBytes(filePath, rec.Read().ToArray());
                    Console.WriteLine($"Extracted to {filePath}");
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                    return 1;
                }
                break;

            case "extract-dir":
                if (internalPath == null || filePath == null)
                {
                    Console.Error.WriteLine("Usage: extract-dir <inputPath> <internalDir> <outDir>");
                    return 1;
                }
                var nodePath = internalPath.TrimEnd('/');
                if (!index.TryFindNode(nodePath, out var node, null))
                {
                    Console.Error.WriteLine("Error: Internal directory not found: " + nodePath);
                    return 1;
                }
                Console.WriteLine($"Extracting directory: {nodePath}");
                int count = LibBundle3.Index.ExtractParallel(node, filePath, (fr, path) =>
                {
                    Console.WriteLine("Extracted: " + path);
                    return false;
                });
                Console.WriteLine($"Done! Extracted {count} files.");
                break;

            case "replace":
                if (internalPath == null || filePath == null)
                {
                    Console.Error.WriteLine("Usage: replace <inputPath> <internalPath> <inFile>");
                    return 1;
                }
                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine($"Error: Input file not found: {filePath}");
                    return 1;
                }
                if (index.TryGetFile(internalPath, out var rec2))
                {
                    rec2.Write(File.ReadAllBytes(filePath));
                    index.Save();
                    Console.WriteLine("Replaced and saved.");
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                    return 1;
                }
                break;

            case "replace-zip":
                if (internalPath == null)
                {
                    Console.Error.WriteLine("Usage: replace-zip <inputPath> <zipFile>");
                    return 1;
                }
                if (!File.Exists(internalPath))
                {
                    Console.Error.WriteLine($"Error: Zip file not found: {internalPath}");
                    return 1;
                }
                Console.WriteLine($"[INFO] Replacing from zip: {internalPath}");
                using (var zip = ZipFile.OpenRead(internalPath))
                {
                    int replacedCount = LibBundle3.Index.Replace(index, zip.Entries, (fr, path) =>
                    {
                        Console.WriteLine($"[OK] Replaced: {path}");
                        return false;
                    });
                    Console.WriteLine($"Done! Replaced {replacedCount} files from zip.");
                }
                break;

            default:
                Console.Error.WriteLine("Unknown command: " + command);
                return 1;
        }
        return 0;
    }
}
