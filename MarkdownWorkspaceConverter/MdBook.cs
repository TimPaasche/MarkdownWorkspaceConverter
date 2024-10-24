namespace MarkdownWorkspaceConverter;
internal class MdBook
{
    internal string Title { get; }

    private readonly string _outputDir;
    private readonly string _srcDir;

    internal MdBook(string title, string outputDir)
    {
        Title = title;
        _outputDir = outputDir;
        _srcDir = outputDir + @"\src";
    }

    internal string Init()
    {
        string command = $"mdbook";
        string arg = $"init --force --title {Title} --ignore git {_outputDir}";
        string result = FancyTerminal.ExecuteCommand(command, arg);

        File.Delete(_outputDir + "/src/chapter_1.md");

        return result;
    }

    internal string Build()
    {
        string command = $"mdbook";
        string args = $"build {_outputDir} -o";
        string result = FancyTerminal.ExecuteCommand(command, args);

        return result;
    }

    internal void CreateSummary()
    {
        string summaryFile = Path.Combine(_srcDir, "SUMMARY.md");
        string summaryContent = "# Summary\n\n";

        summaryContent = AddFilesToSummary(summaryContent, _srcDir, 0);

        File.WriteAllText(summaryFile, summaryContent);
        Console.WriteLine("SUMMARY.md has been updated.");
    }

    private string AddFilesToSummary(string summaryContent, string directory, int level)
    {
        var files = Directory.GetFiles(directory, "*.md");

        if (files.Any(file => Path.GetFileNameWithoutExtension(file).Equals("Home", StringComparison.OrdinalIgnoreCase)))
        {
            summaryContent += $"- [Home](home.md)\n";
        }
        var subDirectories = Directory.GetDirectories(directory);

        List<string> alreadyConvertedSubDirectories = [];
        foreach (var file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file);

            if (filename.Equals("SUMMARY", StringComparison.OrdinalIgnoreCase) ||
                filename.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = file.Substring(_srcDir.Length + 1).Replace("\\", "/");
            string indentation = new string(' ', level * 4);

            summaryContent += $"{indentation}- [{filename}]({relativePath})\n";
            if (subDirectories.Any(subDirectory => Path.GetFileNameWithoutExtension(subDirectory).Equals(filename, StringComparison.OrdinalIgnoreCase)))
            {
                string subDirectory = subDirectories.First(subDirectory => Path.GetFileNameWithoutExtension(subDirectory).Equals(filename, StringComparison.OrdinalIgnoreCase));
                alreadyConvertedSubDirectories.Add(subDirectory);
                summaryContent = AddFilesToSummary(summaryContent, subDirectory, level + 1);
            }
        }

        foreach (var subDirectory in subDirectories.Except(alreadyConvertedSubDirectories))
        {
            summaryContent = AddFilesToSummary(summaryContent, subDirectory, level + 1);
        }

        return summaryContent;
    }
}
