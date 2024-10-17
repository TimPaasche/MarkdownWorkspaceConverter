using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownWorkspaceConverter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            args = [@"C:\Users\DEPAATIM\Desktop\Test\WIKI", @"C:\Users\DEPAATIM\Desktop\Test\WIKI-copy"];
            //args = [@"C:\TFS\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom.wiki_copy"];
#endif
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: MarkdownWorkspaceConverter <srcDir> <dstDir>");
                return;
            }

            string inputDir = args[0];
            string outputDir = args[1];

            FancyTerminal.PrintHeader("TASK", "FILE / PATH");

            if (!Directory.Exists(inputDir))
            {
                FancyTerminal.PrintError("Source directory does not exist", inputDir);
                throw new DirectoryNotFoundException();
            }

            if (!Directory.Exists(outputDir))
            {
                CreateDirectoryAndLog(outputDir);
            }

            foreach (string srcFile in Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories))
            {
                string srcDir = Path.GetDirectoryName(srcFile);
                string dstFile = BuildDstPath(inputDir, outputDir, srcFile);
                string dstDir = Path.GetDirectoryName(dstFile);
                CopyFileAndLog(srcFile, Path.GetDirectoryName(dstFile));
                ConvertMdFileToUtf8(dstFile);
                CreateLocalAttachments(srcDir, srcFile, dstDir, dstFile);
            }

        }

        private static void CreateDirectoryAndLog(string dir)
        {
            ArgumentNullException.ThrowIfNull(dir);

            try
            {
                Directory.CreateDirectory(dir);
                FancyTerminal.PrintSuccess("Directory created", dir);
            }
            catch (Exception)
            {
                FancyTerminal.PrintError("Creation of directory failed", dir);
            }
        }

        private static void CopyFileAndLog(string srcFile, string dstDir)
        {
            ArgumentNullException.ThrowIfNull(srcFile);

            if (!Path.Exists(dstDir))
            {
                CreateDirectoryAndLog(dstDir);
            }

            string fileName = Path.GetFileName(srcFile);

            try
            {
                File.Copy(srcFile, Path.Combine(dstDir, fileName), true);
                FancyTerminal.PrintSuccess("File copied", fileName);
            }
            catch (Exception)
            {
                FancyTerminal.PrintError("Copying of file failed", fileName);
            }
        }

        private static string BuildDstPath(string inputDir, string outputDir, string filePath)
        {
            return filePath.Replace(inputDir, outputDir);
        }

        private static void CreateLocalAttachments(string srcDir, string srcFile, string dstDir, string dstFile)
        {
            string attachmentDir = dstDir + @"\.attachment-" + Path.GetFileNameWithoutExtension(dstFile);
            CreateDirectoryAndLog(attachmentDir);

            string regexPattern = @"!\[.*?\]\((.*?)\)";
            string markdownContent = File.ReadAllText(dstFile);

            MatchCollection regexMatches = Regex.Matches(markdownContent, regexPattern);

            List<(string oldPath, string newPath)> paths = [];
            foreach (Match match in regexMatches)
            {
                if (!match.Success) { continue; }
                string path = match.Groups[1].Value;

                if (path.StartsWith("Http") || path.StartsWith("http")) { continue; }

                FancyTerminal.PrintInfo("Found file refernce", path);
                if (Path.IsPathRooted(path))
                {
                    if (!path.StartsWith('/') && !path.StartsWith('\\'))
                    {
                        FancyTerminal.PrintWarning("A path in *.md-file is absolute", dstFile);
                    }

                    string srcAttachmentPath = Path.Combine(srcDir, path.TrimStart('/').TrimStart('\\'));
                    CopyFileAndLog(srcAttachmentPath, attachmentDir);
                    string newPath = @".attachment-" + Path.GetFileNameWithoutExtension(dstFile) + @"\" + Path.GetFileName(path);
                    FancyTerminal.PrintInfo("refactor link", newPath);
                    paths.Add((path, newPath));
                }
                else
                {
                    string srcAttachmentPath = Path.Combine(srcDir, path);
                    CopyFileAndLog(srcAttachmentPath, attachmentDir);
                    string newPath = @".attachment-" + Path.GetFileNameWithoutExtension(dstFile) + @"\" + Path.GetFileName(path);
                    FancyTerminal.PrintInfo("refactor link", newPath);
                    paths.Add((path, newPath));
                }
            }
            string content = File.ReadAllText(dstFile);
            foreach ((string oldPath, string newPath) path in paths)
            {
                content = content.Replace(path.oldPath, path.newPath);
            }
            File.WriteAllText(dstFile, content);
        }

        private static void ConvertMdFileToUtf8(string mdFile)
        {
            string content = File.ReadAllText(mdFile, Encoding.Default);
            content = FormatMarkdownHeadlines(content);
            content = FormatMarkdownTables(content);
            File.WriteAllText(mdFile, content, Encoding.UTF8);
            FancyTerminal.PrintSuccess("*.md-file to utf8 converted", mdFile);
        }

        private static string FormatMarkdownHeadlines(string mdStream)
        {
            string pattern = @"(#+)([\w]+)";
            string replacement = @"$1 $2";
            string result = Regex.Replace(mdStream, pattern, replacement);

            return result;
        }


        private static string FormatMarkdownTables(string mdStream)
        {
            // FIX MISSING '|' CHAR
            //string patternMissingEndlineChar = @"^\|.*[^|\s]\s*$";
            //MatchCollection result = Regex.Matches(mdStream, patternMissingEndlineChar, RegexOptions.Multiline);
            //foreach (Match match in result)
            //{

            //    string newLine = $"{match.Groups[1].Value.TrimEnd()} |";
            //    mdStream = mdStream.Replace(match.Groups[0].Value, newLine);
            //}

            // FIX SEPERATOR COUNT
            string pattern = @"(\|[^\n]+\|\r?\n)((?:\|:?[-]+:?)+\|)";
            MatchCollection regexMatches = Regex.Matches(mdStream, pattern);

            foreach (Match match in regexMatches)
            {
                int countHeader = match.Groups[1].Value.Split('|').Length - 2; // Adjusted to get the correct count
                FancyTerminal.PrintInfo(task: $"Count Columns (header): {countHeader}", location: "x");
                int countSeparator = match.Groups[2].Value.Split('|').Length - 2; // Adjusted to get the correct count
                FancyTerminal.PrintInfo(task: $"Count Columns (separator): {countSeparator}", location: "x");
                for (int i = 0; i < countSeparator - countHeader; i++)
                {
                    var firstHalf = mdStream.Substring(0, match.Groups[2].Index);
                    var secondHalf = mdStream.Substring(match.Groups[2].Index).TrimStart('|').TrimStart('-');

                    mdStream = firstHalf + secondHalf;
                }
            }

            return mdStream;
        }

    }
}
