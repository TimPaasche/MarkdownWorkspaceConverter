using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownWorkspaceConverter;

internal class Program
{
    private static void Main(string[] args)
    {
#if DEBUG
        //args = [@"C:\Users\DEPAATIM\Desktop\Test\WIKI", @"C:\Users\DEPAATIM\Desktop\Test\WIKI-copy"];
        //args = [@"C:\Users\DEPAATIM\Desktop\Test\WIKI", @"C:\Users\DEPAATIM\Desktop\Test\WIKI-copy", "--mdBook"];
        args = [@"D:\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom_wiki_copy"];
#endif
        if (args.Length < 2 || args.Length > 3)
        {
            Console.WriteLine("Usage: MarkdownWorkspaceConverter <srcDir> <dstDir> <optionFlag>");
            return;
        }

        string inputDir = args[0];
        string outputDir = args[1];
        string optionFlag = args.Length == 3
            ? args[2]
            : string.Empty;

        string title = Path.GetFileNameWithoutExtension(inputDir);

        FancyTerminal.PrintHeader("TASK", "FILE / PATH");

        if (!Directory.Exists(inputDir))
        {
            FancyTerminal.PrintError("Source directory does not exist", inputDir);
            throw new DirectoryNotFoundException();
        }

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
            CreateDirectoryAndLog(outputDir);
        }
        else
        {
            CreateDirectoryAndLog(outputDir);
        }

        MdBook mdBook = new(title, outputDir);
        mdBook.Init();
        outputDir += @"\src";

        foreach (string srcFile in Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories))
        {
            string srcDir = Path.GetDirectoryName(srcFile);
            string dstFile = BuildDstPath(inputDir, outputDir, srcFile);
            string dstDir = Path.GetDirectoryName(dstFile);
            CopyFileAndLog(srcFile, Path.GetDirectoryName(dstFile));
            ConvertMdFileToUtf8(dstFile);
            CreateLocalAttachments(srcDir, srcFile, dstDir, dstFile, inputDir);
        }


        mdBook.CreateSummary();
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

    private static void CreateLocalAttachments(string srcDir, string srcFile, string dstDir, string dstFile, string rootDir)
    {
        const string ATTACHMENT_PATH = "_attachment-";

        string attachmentDir = dstDir + $@"\{ATTACHMENT_PATH}" + Path.GetFileNameWithoutExtension(dstFile);
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

            FancyTerminal.PrintInfo("Found file reference", path);
            if (Path.IsPathRooted(path))
            {
                if (!path.StartsWith('/') && !path.StartsWith('\\'))
                {
                    FancyTerminal.PrintWarning("A path in *.md-file is absolute", dstFile);
                }

                string srcAttachmentPath = Path.Combine(rootDir, path.TrimStart('/').TrimStart('\\').Replace('/', '\\'));
                CopyFileAndLog(srcAttachmentPath, attachmentDir);
                FancyTerminal.PrintInfo("Src path copy", srcAttachmentPath);
                string newPath = $@"{ATTACHMENT_PATH}" + Path.GetFileNameWithoutExtension(dstFile) + "/" + Path.GetFileName(path);
                FancyTerminal.PrintInfo("refactor link", newPath);
                paths.Add((path, newPath));
            }
            else
            {
                string srcAttachmentPath = Path.Combine(srcDir, path.TrimStart('/').TrimStart('\\').Replace('/', '\\'));
                CopyFileAndLog(srcAttachmentPath, attachmentDir);
                string newPath = $@"{ATTACHMENT_PATH}" + Path.GetFileNameWithoutExtension(dstFile) + "/" + Path.GetFileName(path);
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
        content = content.FormatMarkdownHeadlines();
        content = content.FormatMarkdownTables();
        File.WriteAllText(mdFile, content, Encoding.UTF8);
        FancyTerminal.PrintSuccess("*.md-file to utf8 converted", mdFile);
    }
}
