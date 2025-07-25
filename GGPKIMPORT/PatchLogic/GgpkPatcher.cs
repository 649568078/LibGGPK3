using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using LibBundledGGPK3;

namespace GGPKIMPORT.PatchLogic
{
    public static class GgpkPatcher
    {
        public static int Patch(string ggpkPath, string patchZipName)
        {
            if (!File.Exists(ggpkPath))
                throw new FileNotFoundException("GGPK 文件不存在", ggpkPath);

            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + patchZipName, StringComparison.OrdinalIgnoreCase));

                        // 🧪 加入调试输出：
            Console.WriteLine("嵌入资源列表：");
            foreach (var res in asm.GetManifestResourceNames())
            {
                Console.WriteLine(" - " + res);
            }
            Console.WriteLine("查找补丁名: " + patchZipName);
            Console.WriteLine("匹配到资源名: " + resourceName);

            if (resourceName == null)
                throw new FileNotFoundException("找不到嵌入补丁资源", patchZipName);

            using var zipStream = asm.GetManifestResourceStream(resourceName)!;
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
            using var ggpk = new BundledGGPK(ggpkPath, false);

            return LibBundle3.Index.Replace(ggpk.Index, zip.Entries, (fr, path) =>
            {
                Console.WriteLine("Replaced: " + path);
                return false;
            });
        }
    }
}