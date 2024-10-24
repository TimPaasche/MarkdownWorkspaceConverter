using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownWorkspaceConverter;

internal class Program
{
    private const bool ADVANCED_LOGGING = false;

    private static void Main(string[] args)
    {
#if DEBUG
        //args = [@"C:\Users\DEPAATIM\Desktop\Test\WIKI", @"C:\Users\DEPAATIM\Desktop\Test\WIKI-copy", "--pandoc-html"];
        //args = [@"C:\Users\DEPAATIM\Desktop\Test\WIKI", @"C:\Users\DEPAATIM\Desktop\Test\WIKI-copy", "--mdBook"];
        //args = [@"D:\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom_wiki_copy", "--pandoc-html", "--debug"];
        args = [@"D:\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom_wiki_copy", "--pandoc-html"];
        //args = [@"D:\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom_wiki_copy", "--mdbook"];
        //args = [@"D:\Repository\Inno_ICom.wiki", @"C:\Users\DEPAATIM\Desktop\Test\Inno_ICom_wiki_copy", "--pandoc-typst"];
        //args = [@"-h"];
#endif
        if (args.Length == 1 && (args.First() == "--help" || args.First() == "-h"))
        {
            Console.WriteLine("Usage: MarkdownWorkspaceConverter <srcDir> <dstDir> <flag>");
            Console.WriteLine("Flags: --mdbook or --pandoc-html");
            return;
        }

        if (args.Length < 2 || args.Length > 4)
        {
            Console.WriteLine("Usage: MarkdownWorkspaceConverter <srcDir> <dstDir> <flag>");
            return;
        }

        string inputDir = args[0];
        string outputDir = args[1];
        string[] flags = [];
        if (args.Length > 2)
        {
            flags = args.ToList().Where(arg => (arg != inputDir || arg != outputDir)).ToArray();
        }

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



        if (flags.Contains("--mdbook"))
        {
            ConvertViaMdBook(inputDir, outputDir, title);
            return;
        }

        if (flags.Contains("--pandoc-html"))
        {
            if (flags.Contains("--debug"))
            {
                ConvertViaPandocHtml(inputDir, outputDir, title, debug: true);
                return;
            }
            ConvertViaPandocHtml(inputDir, outputDir, title);
            return;
        }

        if (flags.Contains("--pandoc-typst"))
        {
            ConvertViaPandocTypst(inputDir, outputDir, title);
            return;
        }
    }

    private static void CreateDirectoryAndLog(string dir)
    {
        ArgumentNullException.ThrowIfNull(dir);

        try
        {
            Directory.CreateDirectory(dir);
            if (ADVANCED_LOGGING) { FancyTerminal.PrintSuccess("Directory created", dir); }
        }
        catch (Exception)
        {
            FancyTerminal.PrintError("Creation of directory failed", dir);
        }
    }

    private static void CopyFileAndLog(string srcFile, string dstFile)
    {
        ArgumentNullException.ThrowIfNull(srcFile);

        string dstDir = Path.GetDirectoryName(dstFile);

        if (!Path.Exists(dstDir))
        {
            CreateDirectoryAndLog(dstDir);
        }

        try
        {
            File.Copy(srcFile, Path.Combine(dstDir, dstFile), true);
            if (ADVANCED_LOGGING) { FancyTerminal.PrintSuccess("File copied", Path.GetFileName(dstFile)); }
        }
        catch (Exception)
        {
            FancyTerminal.PrintError("Copying of file failed", Path.GetFileName(dstFile));
        }
    }

    private static string BuildDstPath(string inputDir, string outputDir, string filePath)
    {
        return filePath.Replace(inputDir, outputDir);
    }

    private static void ConvertMdFileToUtf8(string mdFile, bool refactorHorizontalLines = false)
    {
        string content = File.ReadAllText(mdFile, Encoding.Default);
        content = content.FormatMarkdownHeadlines();
        content = content.FormatMarkdownTables();
        content = refactorHorizontalLines
            ? content.RefactorHorizontalLines()
            : content;
        content = content.FormatImagesLiningSpaces();
        File.WriteAllText(mdFile, content, Encoding.UTF8);
        if (ADVANCED_LOGGING) { FancyTerminal.PrintSuccess("*.md-file to utf8 converted", mdFile); }
    }

    private static void RefactorAttachments(string srcDir, string srcFile, string dstDir, string dstFile, string rootDir)
    {
        const string ATTACHMENT_PREFIX = "_attachment";

        string attachmentDir = dstDir.Replace("%2D", "-") + $@"\{ATTACHMENT_PREFIX}" + @"\" + Path.GetFileNameWithoutExtension(dstFile.Replace("%2D", "-"));
        CreateDirectoryAndLog(attachmentDir);

        string regexPattern = @"!\[.*?\]\((.*?)\)";
        string markdownContent = File.ReadAllText(dstFile);

        MatchCollection regexMatches = Regex.Matches(markdownContent, regexPattern);
        markdownContent = Regex.Replace(markdownContent, regexPattern, match => ReplaceImagePathMatch(match, srcDir, rootDir, srcFile, attachmentDir, dstFile, ATTACHMENT_PREFIX));

        File.WriteAllText(dstFile, markdownContent);
    }

    private static string RefactorImagePaths(string mdFilePath, string dstAttachmentDirPath, string srcAttachmentDirPath, string imageFilePath, string attachmentPrefix)
    {
        string srcAttachmentPath = Path.Combine(srcAttachmentDirPath, imageFilePath.TrimStart('/').TrimStart('\\').Replace('/', '\\'));
        string dstImagePath = dstAttachmentDirPath + "/" + Path.GetFileName(imageFilePath);
        CopyFileAndLog(srcAttachmentPath, dstImagePath);
        string newPath = $@"{attachmentPrefix}" + "/" + Path.GetFileNameWithoutExtension(mdFilePath) + "/" + Path.GetFileName(imageFilePath);
        if (ADVANCED_LOGGING) { FancyTerminal.PrintInfo("refactor link", newPath); }
        return newPath.Replace("%2D", "-");
    }

    private static string ReplaceImagePathMatch(Match match, string srcDir, string rootDir, string srcFile, string attachmentDir, string mdFile, string attachmentPrefix)
    {
        string oldPath = match.Groups[1].Value;

        if (oldPath.StartsWith("Http") || oldPath.StartsWith("http"))
        {
            return match.Value;
        }

        if (ADVANCED_LOGGING) { FancyTerminal.PrintInfo("Found file reference", oldPath); }
        if (Path.IsPathRooted(oldPath))
        {
            string newPathLocal = RefactorImagePaths(
               srcAttachmentDirPath: rootDir,
               imageFilePath: oldPath,
               dstAttachmentDirPath: attachmentDir,
               mdFilePath: mdFile,
               attachmentPrefix: attachmentPrefix);
            string resultLocal = match.Value.Replace(oldPath, newPathLocal);
            return resultLocal;
        }
        string newPath = RefactorImagePaths(
            srcAttachmentDirPath: srcDir,
            imageFilePath: oldPath,
            dstAttachmentDirPath: attachmentDir,
            mdFilePath: mdFile,
            attachmentPrefix: attachmentPrefix);
        string result = match.Value.Replace(oldPath, newPath);
        return result;
    }

    private static void RefectorFileLinks(string dstDir, string dstFile, string dstRootDir, string fileExtension = ".md")
    {
        string markdownContent = File.ReadAllText(dstFile);
        string relativePath = Path.GetRelativePath(dstDir, dstRootDir);

        //string regexPattern = @"\((\/.*?\(.*?\))\)|\((\/.*?)\)";
        //string regexPattern = @"\[\s*?!\[.*?\].*?\]\((\/.*?\(.*?\))\)|\[\s*?!\[.*?\].*?\]\((\/?.*?)\)|\[.*?\]\((\/.*?\(.*?\))\)|\[.*?\]\((\/?.*?)\)";
        string regexPattern = @"(?<!!)\[\s*?!\[.*?\].*?\]\((\/.*?\(.*?\))\)|(?<!!)\[\s*?!\[.*?\].*?\]\((\/?.*?)\)|(?<!!)\[.*?\]\((\/.*?\(.*?\))\)|(?<!!)\[.*?\]\((\/?.*?)\)";
        markdownContent = Regex.Replace(markdownContent, regexPattern, match => ReplaceRelativeLinkMatch(match, relativePath, fileExtension));

        File.WriteAllText(dstFile, markdownContent);
    }

    private static string ReplaceRelativeLinkMatch(Match match, string relativePath, string fileExtension)
    {
        string oldPath = !String.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[1].Value
                : !String.IsNullOrEmpty(match.Groups[2].Value)
                    ? match.Groups[2].Value
                    : !String.IsNullOrEmpty(match.Groups[3].Value)
                        ? match.Groups[3].Value
                        : match.Groups[4].Value;

        if (oldPath.StartsWith("Http") || oldPath.StartsWith("http")) { return match.Value; }
        if (String.IsNullOrEmpty(oldPath)) { return match.Value; }

        string newPath = relativePath.Replace('\\', '/') + "/" + oldPath.TrimStart('\\').TrimStart('/') + fileExtension;
        newPath = newPath.Replace("%2D", "-");
        string result = match.Value.Replace(oldPath, newPath);
        return result;
    }

    private static void ConvertViaMdBook(string inputDir, string outputDir, string title)
    {

        MdBook mdBook = new(title, outputDir);
        mdBook.Init();
        outputDir += @"\src";

        foreach (string srcFile in Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories))
        {
            string srcDir = Path.GetDirectoryName(srcFile);
            string dstFile = BuildDstPath(inputDir, outputDir, srcFile).Replace("%2D", "-");
            string dstDir = Path.GetDirectoryName(dstFile);
            CopyFileAndLog(srcFile, dstFile);
            ConvertMdFileToUtf8(dstFile);
            RefactorAttachments(srcDir, srcFile, dstDir, dstFile, inputDir);
            RefectorFileLinks(dstDir, dstFile, outputDir);
        }

        mdBook.CreateSummary();
        mdBook.Build();
    }

    private static void ConvertViaPandocHtml(string inputDir, string outputDir, string title, bool debug = false)
    {
        foreach (string srcFiles in Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories))
        {
            string srcFilePath = srcFiles;
            string srcDirPath = Path.GetDirectoryName(srcFilePath);
            string dstFilePath = BuildDstPath(inputDir, outputDir, srcFilePath).Replace("%2D", "-");
            string dstDirPath = Path.GetDirectoryName(dstFilePath);
            CopyFileAndLog(srcFilePath, dstFilePath);
            ConvertMdFileToUtf8(dstFilePath, true);
            RefactorAttachments(srcDirPath, srcFilePath, dstDirPath, dstFilePath, inputDir);
            RefectorFileLinks(dstDirPath, dstFilePath, outputDir, ".html");
            string content = File.ReadAllText(dstFilePath, Encoding.Default);
            content = content.RefactorTOC();
            File.WriteAllText(dstFilePath, content);

            string pandocDstFilePath = dstDirPath + "\\" + Path.GetFileNameWithoutExtension(dstFilePath) + ".html";
            string command = $"pandoc";
            string cssPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\style.css";
            string args = @$"-s {dstFilePath} -o {pandocDstFilePath} --from gfm --to html5 -c {cssPath}";
            string result = FancyTerminal.ExecuteCommand(command, args);
            if (!debug) { File.Delete(dstFilePath); }
            FancyTerminal.PrintSuccess("converted to html", Path.GetFileNameWithoutExtension(dstFilePath));
        }
        return;
    }

    private static void ConvertViaPandocTypst(string inputDir, string outputDir, string title)
    {
        foreach (string srcFiles in Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories))
        {
            string srcFilePath = srcFiles;
            string srcDirPath = Path.GetDirectoryName(srcFilePath);
            string dstFilePath = BuildDstPath(inputDir, outputDir, srcFilePath).Replace("%2D", "-");
            string dstDirPath = Path.GetDirectoryName(dstFilePath);
            CopyFileAndLog(srcFilePath, dstFilePath);
            ConvertMdFileToUtf8(dstFilePath, true);
            RefactorAttachments(srcDirPath, srcFilePath, dstDirPath, dstFilePath, inputDir);
            RefectorFileLinks(dstDirPath, dstFilePath, outputDir, ".pdf");

            string pandocDstFilePath = dstDirPath + "\\" + Path.GetFileNameWithoutExtension(dstFilePath) + ".typ";
            string command = $"pandoc";
            string args = @$"-s {dstFilePath} -o {pandocDstFilePath} --from gfm --to typst";
            string result = FancyTerminal.ExecuteCommand(command, args);
            File.Delete(dstFilePath);
            FancyTerminal.PrintSuccess("converted to typst", Path.GetFileNameWithoutExtension(dstFilePath));
        }
        return;
    }
}
