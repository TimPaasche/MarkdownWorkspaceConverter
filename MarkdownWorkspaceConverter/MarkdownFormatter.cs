using System.Text.RegularExpressions;

namespace MarkdownWorkspaceConverter;
internal static class MarkdownFormatter
{

    internal static string FormatMarkdownHeadlines(this string mdStream)
    {
        string pattern = @"^(#+)([\w\-\[!])";
        Regex regex = new(pattern, RegexOptions.Multiline);
        string replacement = @"$1 $2";
        string result = regex.Replace(mdStream, replacement);

        return result;
    }

    internal static string FormatMarkdownTables(this string mdStream)
    {
        // Optional, test if necessary
        // FIX MISSING '|' CHAR
        //string patternMissingEndlineChar = @"^\|.*[^|\s]\s*$";
        //MatchCollection result = Regex.Matches(mdStream, patternMissingEndlineChar, RegexOptions.Multiline);
        //foreach (Match match in result)
        //{

        //    string newLine = $"{match.Groups[1].Value.TrimEnd()} |";
        //    mdStream = mdStream.Replace(match.Groups[0].Value, newLine);
        //}

        // FIX SEPERATOR COUNT
        string pattern = @"(\|[^\n]+\|?\r?\n)((?:\|:?[-]+:?)+\|)";
        MatchCollection regexMatches = Regex.Matches(mdStream, pattern);

        foreach (Match match in regexMatches)
        {
            //string findCellRegexPattern = @"\|(?:([^|]*)\|?)*\n";
            string findCellRegexPattern = @"\|([^|\n\r]+)";

            int headerMatchesCount = Regex.Matches(match.Groups[1].Value, findCellRegexPattern).Count;
            int separatorMatchesCount = Regex.Matches(match.Groups[2].Value, findCellRegexPattern).Count;

            for (int i = 0; i < separatorMatchesCount - headerMatchesCount; i++)
            {
                var firstHalf = mdStream.Substring(0, match.Groups[2].Index);
                var secondHalf = mdStream.Substring(match.Groups[2].Index).TrimStart('|').TrimStart('-');

                mdStream = firstHalf + secondHalf;
            }
        }

        return mdStream;
    }

    internal static string FormatImagesLiningSpaces(this string mdStream)
    {
        string pattern = @"(?<=\r\n)!\[.+?\]\(.*?\)(?=\r\n)|(?<=\n)!\[.+?\]\(.*?\)(?=\n)";
        string replacement = $"\n$&\n";
        Regex regex = new Regex(pattern);
        return regex.Replace(mdStream, replacement);
    }

    internal static string RefactorHorizontalLines(this string mdStream)
    {
        string pattern = @"(?<=\r\n)-+(?=\r\n)|(?<=\n)-+(?=\n)";
        string replacement = "<hr>";
        Regex regex = new Regex(pattern);
        return regex.Replace(mdStream, replacement);
    }

    internal static string RefactorTOC(this string mdStream)
    {
        if (!mdStream.Contains("[[_TOC_]]"))
        {
            return mdStream;
        }

        string pattern = @"^(#+)\s+(.+)$";
        Regex regex = new(pattern, RegexOptions.Multiline);
        MatchCollection matches = regex.Matches(mdStream);

        string toc = "# Table Of Contents:\n\n";
        foreach (Match match in matches)
        {
            int indents = match.Groups[1].Length - 1;
            string headline = match.Groups[2].Value.Trim();
            string refernceLink = headline.ToLower().Replace(' ', '-').Trim();
            for (int i = 0; i < indents; i++)
            {
                toc += "\t";
            }
            toc += "- ";
            toc += $"[{headline}](#{refernceLink})\n";
        }
        toc += "<hr>\n<br>\n<br>\n";
        mdStream = mdStream.Replace("[[_TOC_]]", toc);
        return mdStream;
    }
}
