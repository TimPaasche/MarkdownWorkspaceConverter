using System;
using System.IO;

namespace MarkdownWorkspaceConverter {
    internal class Program {
        private static void Main(string[] args) {
#if DEBUG
            args = new[] { @"C:\Users\TLP-PC\Desktop\Envirement_for_testing\MarkdownWorkspace", @"C:\Users\TLP-PC\Desktop\Envirement_for_testing\MarkdownWorkspace_Copy" };
#endif
            if (args.Length != 2) {
                Console.WriteLine("Usage: MarkdownWorkspaceConverter <srcDir> <dstDir>");
                return;
            }

            string srcDir = args[0];
            string dstDir = args[1];

            FancyTerminal.PrintHeader("TASK", "FILE / PATH");

            if (!Directory.Exists(srcDir)) {
                FancyTerminal.PrintError("Source directory does not exist", srcDir);
                throw new DirectoryNotFoundException();
            }

            if (!Directory.Exists(dstDir)) {
                CreateDirectoryAndLog(dstDir);
            }

            foreach (string srcFile in Directory.EnumerateFiles(srcDir, "*.md", SearchOption.AllDirectories)) {
                CopyFileAndLog(srcFile, dstDir);
            }
        }

        private static void CreateDirectoryAndLog(string dir) {
            ArgumentNullException.ThrowIfNull(dir);

            try {
                Directory.CreateDirectory(dir);
                FancyTerminal.PrintSuccess("Directory created", dir);
            } catch (Exception) {
                FancyTerminal.PrintError("Creation of directory failed", dir);
            }
        }

        private static void CopyFileAndLog(string srcFile, string dstDir) {
            ArgumentNullException.ThrowIfNull(srcFile);

            string fileName = Path.GetFileName(srcFile);

            try {
                File.Copy(srcFile, Path.Combine(dstDir, fileName), true);
                FancyTerminal.PrintSuccess("File copied", fileName);
            } catch (Exception) {
                FancyTerminal.PrintError("Copying of file failed", fileName);
            }
        }
    }
}
