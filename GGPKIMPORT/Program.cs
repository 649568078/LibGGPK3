using System;
using System.IO;
using System.Reflection;
using GGPKIMPORT.PatchLogic;

namespace GGPKIMPORT;
public static class Program
{
    public static int Main(string[] args)
    {
        var pause = false;
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            Console.WriteLine($"GGPKIMPORT v{version.Major}.{version.Minor}.{version.Build}");
            Console.WriteLine();

            string ggpkPath, patchName;

            if (args.Length == 0)
            {
                pause = true;
                Console.Write("Path to Content.ggpk: ");
                ggpkPath = Console.ReadLine()!;
                Console.Write("Patch zip name (e.g. patch1.zip): ");
                patchName = Console.ReadLine()!;
            }
            else if (args.Length == 1)
            {
                ggpkPath = args[0];
                patchName = "patch1.zip";
            }
            else if (args.Length == 2)
            {
                ggpkPath = args[0];
                patchName = args[1];
            }
            else
            {
                Console.WriteLine("Usage: GGPKIMPORT <PathToGGPK> [PatchZipName]");
                Console.WriteLine("Enter to exit . . .");
                Console.ReadLine();
                return 1;
            }

            if (!File.Exists(ggpkPath))
            {
                Console.WriteLine("FileNotFound: " + ggpkPath);
                Console.WriteLine("Enter to exit . . .");
                Console.ReadLine();
                return 1;
            }

            Console.WriteLine("GGPK: " + ggpkPath);
            Console.WriteLine("Patch: " + patchName);
            Console.WriteLine("Reading ggpk file . . .");

            int count = GgpkPatcher.Patch(ggpkPath, patchName);

            Console.WriteLine($"✅ Done! Replaced {count} files.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("❌ Error: " + e);
            return 1;
        }
        finally
        {
            if (pause)
            {
                Console.WriteLine();
                Console.WriteLine("Enter to exit . . .");
                Console.ReadLine();
            }
        }
    }
}