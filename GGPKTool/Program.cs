using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
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
            Console.WriteLine("  GGPKTool.exe replace-zenc-zip <ggpkOrIndexPath> <zencFile>");
            Console.WriteLine("  GGPKTool.exe extract-dir <ggpkOrIndexPath> <internalDir> <outDir>");
            return 1;
        }

        string command = args[0].ToLower();
        string inputPath = args.Length > 1 ? args[1] : "";
        string? internalPath = args.Length > 2 ? args[2] : null;
        string? filePath = args.Length > 3 ? args[3] : null;

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
                using var index = new LibBundle3.Index(inputPath, false);
                index.ParsePaths();
                return RunCommand(index, isSteam, command, internalPath, filePath);
            }
            else
            {
                using var ggpk = new BundledGGPK(inputPath, false);
                var index = ggpk.Index;
                index.ParsePaths();
                return RunCommand(index, isSteam, command, internalPath, filePath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine();
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey(true);
                }
            }
            catch { }
        }
    }

    static int RunCommand(LibBundle3.Index index, bool isSteam, string command, string? internalPath, string? filePath)
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
                    int replacedCount;
                    if (isSteam)
                    {
                        // Steam/Epic 模式，重新打包 bundle
                        replacedCount = LibBundle3.Index.ReplaceInPlace(index, zip.Entries, (fr, path) =>
                        {
                            Console.WriteLine($"[OK] Replaced: {path}");
                            return false;
                        });
                    }
                    else
                    {
                        // GGPK 模式，走原有 Replace
                        replacedCount = LibBundle3.Index.Replace(index, zip.Entries, (fr, path) =>
                        {
                            Console.WriteLine($"[OK] Replaced: {path}");
                            return false;
                        });
                    }
                    Console.WriteLine($"Done! Replaced {replacedCount} files from zip.");
                }
                break;

            case "replace-zenc-zip":
                if (internalPath == null)
                {
                    Console.Error.WriteLine("Usage: replace-zenc-zip <inputPath> <zencFile>");
                    return 1;
                }
                if (!File.Exists(internalPath))
                {
                    Console.Error.WriteLine($"Error: Zenc file not found: {internalPath}");
                    return 1;
                }
                Console.WriteLine($"[INFO] Replacing from zenc: {internalPath}");

                const string PASSWORD = "ABCabc123!@#$%^yuioqYUOIMLNCd";
                byte[] plainZip = DecryptZencToBytes(internalPath, PASSWORD);

                using (var ms = new MemoryStream(plainZip, writable: false))
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false))
                {
                    int replacedCount;
                    if (isSteam)
                    {
                        // Steam/Epic 模式
                        replacedCount = LibBundle3.Index.ReplaceInPlace(index, zip.Entries, (fr, path) =>
                        {
                            Console.WriteLine($"[OK] Replaced: {path}");
                            return false;
                        });
                    }
                    else
                    {
                        // GGPK 模式
                        replacedCount = LibBundle3.Index.Replace(index, zip.Entries, (fr, path) =>
                        {
                            Console.WriteLine($"[OK] Replaced: {path}");
                            return false;
                        });
                    }
                    Console.WriteLine($"Done! Replaced {replacedCount} files from zenc.");
                }

                CryptographicOperations.ZeroMemory(plainZip);
                break;

            default:
                Console.Error.WriteLine("Unknown command: " + command);
                return 1;
        }
        return 0;
    }

    static byte[] DecryptZencToBytes(string zencFile, string password)
    {
        using var fs = File.OpenRead(zencFile);

        // 1) MAGIC
        Span<byte> magic = stackalloc byte[5];
        if (fs.Read(magic) != 5 || !magic.SequenceEqual(Encoding.ASCII.GetBytes("ZENC1")))
            throw new InvalidDataException("不是有效的 .zenc 文件");

        // 2) header length
        Span<byte> lenBuf = stackalloc byte[4];
        if (fs.Read(lenBuf) != 4)
            throw new InvalidDataException("缺少 header 长度");
        int hlen = (lenBuf[0] << 24) | (lenBuf[1] << 16) | (lenBuf[2] << 8) | lenBuf[3];

        // 3) header JSON
        byte[] headerBytes = new byte[hlen];
        ReadExact(fs, headerBytes, 0, hlen);

        using var doc = JsonDocument.Parse(headerBytes);
        var root = doc.RootElement;

        int v = root.GetProperty("v").GetInt32();
        string kdf = root.GetProperty("kdf").GetString() ?? "";
        if (v != 1 || !kdf.Equals("pbkdf2-sha256", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("补丁失效，请在群文件下载最新版");

        int iterations = root.GetProperty("iterations").GetInt32();
        byte[] salt = Convert.FromBase64String(root.GetProperty("salt").GetString()!);
        byte[] nonce = Convert.FromBase64String(root.GetProperty("nonce").GetString()!);

        // 4) ciphertext = cipher + tag
        byte[] ct = ReadToEnd(fs);
        if (ct.Length < 16) throw new InvalidDataException("密文长度不足");

        int cipherLen = ct.Length - 16;
        byte[] cipher = new byte[cipherLen];
        byte[] tag = new byte[16];
        Buffer.BlockCopy(ct, 0, cipher, 0, cipherLen);
        Buffer.BlockCopy(ct, cipherLen, tag, 0, 16);

        // 5) derive key with PBKDF2-SHA256
        byte[] key;
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            key = pbkdf2.GetBytes(32);
        }

        // 6) decrypt AES-GCM
        byte[] plaintext = new byte[cipherLen];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipher, tag, plaintext, Encoding.UTF8.GetBytes("zenc-v1"));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }

        return plaintext;
    }

    static void ReadExact(Stream s, byte[] buffer, int offset, int count)
    {
        int read;
        while (count > 0 && (read = s.Read(buffer, offset, count)) > 0)
        {
            offset += read;
            count -= read;
        }
        if (count != 0) throw new EndOfStreamException();
    }

    static byte[] ReadToEnd(Stream s)
    {
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
