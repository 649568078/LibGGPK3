using System;
using System.IO;
using System.IO.Compression;
using LibBundledGGPK3;
using LibBundle3.Nodes;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  GGPKTool.exe extract <ggpkPath> <internalPath> <outFile>");
            Console.WriteLine("  GGPKTool.exe replace <ggpkPath> <internalPath> <inFile>");
            Console.WriteLine("  GGPKTool.exe replace-zip <ggpkPath> <zipFile>");
            Console.WriteLine("  GGPKTool.exe extract-dir <ggpkPath> <internalDir> <outDir>");
            return 1;
        }

        string command = args[0].ToLower();
        string ggpkPath = args[1];
        string internalPath = args.Length > 2 ? args[2] : null;
        string filePath = args.Length > 3 ? args[3] : null;

        switch (command)
        {
            case "extract":
                if (args.Length != 4)
                {
                    Console.WriteLine("Usage: extract <ggpkPath> <internalPath> <outFile>");
                    return 1;
                }
                break;
            case "replace":
                if (args.Length != 4)
                {
                    Console.WriteLine("Usage: replace <ggpkPath> <internalPath> <inFile>");
                    return 1;
                }
                break;
            case "replace-zip":
                if (args.Length != 3)
                {
                    Console.WriteLine("Usage: replace-zip <ggpkPath> <zipFile>");
                    return 1;
                }
                break;
            case "extract-dir":
                if (args.Length != 3)
                {
                    Console.WriteLine("Usage: extract-dir <ggpkPath> <internalDir> <outDir>");
                    return 1;
                }
                break;
            default:
                Console.WriteLine("Unknown command: " + command);
                return 1;
        }

        if (!File.Exists(ggpkPath))
        {
            Console.Error.WriteLine($"Error: GGPK file not found: {ggpkPath}");
            return 1;
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
                    if (filePath == null)
                    {
                        Console.Error.WriteLine("Error: Output path is missing.");
                        return 1;
                    }
                    File.WriteAllBytes(filePath, rec.Read().ToArray());
                    Console.WriteLine($"Extracted to {filePath}");
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                }
            }
            else if (command == "extract-dir")
            {
                if (internalPath == null)
                {
                    Console.Error.WriteLine("Error: Internal directory path missing.");
                    return 1;
                }
                var nodePath = internalPath.TrimEnd('/');
                if (!index.TryFindNode(nodePath, out var node, null))
                {
                    Console.Error.WriteLine("Error: Internal directory not found: " + nodePath);
                    return 1;
                }

                Console.WriteLine($"Extracting directory: {nodePath}");
                if (filePath == null)
                {
                    Console.Error.WriteLine("Error: Output directory missing.");
                    return 1;
                }
                int count = LibBundle3.Index.ExtractParallel(node, filePath, (fr, path) =>
                {
                    Console.WriteLine("Extracted: " + path);
                    return false;
                });
                    Console.WriteLine($"Done! Extracted {count} files.");
            }
            else if (command == "replace")
            {
                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine($"Error: Input file not found: {filePath}");
                    return 1;
                }
                if (index.TryGetFile(internalPath, out var rec))
                {
                    var data = File.ReadAllBytes(filePath);
                    rec.Write(data);
                    index.Save();
                    Console.WriteLine("Replaced and saved.");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("Error: Internal file not found: " + internalPath);
                    return 1;
                }
            }
            else if (command == "replace-zip")
            {
                string zipPath = internalPath; // 参数2：zip文件路径
                if (!File.Exists(zipPath))
                {
                    Console.Error.WriteLine($"Error: Zip file not found: {zipPath}");
                    return 1;
                }

                Console.WriteLine($"[INFO] Replacing from zip: {zipPath}");
                using var zip = ZipFile.OpenRead(zipPath);

                int replacedCount = LibBundle3.Index.Replace(ggpk.Index, zip.Entries, (fr, path) =>
                {
                    Console.WriteLine($"[OK] Replaced: {path}");
                    return false;
                });

                Console.WriteLine($"Done! Replaced {replacedCount} files from zip.");
            }
            else
            {
                Console.Error.WriteLine("Unknown command: " + command);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Exception: " + ex);
            return 1;
        }
        return 0;
    }
}