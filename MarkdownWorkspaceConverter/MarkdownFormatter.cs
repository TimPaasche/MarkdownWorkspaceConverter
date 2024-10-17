using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarkdownWorkspaceConverter;
internal static class MarkdownFormatter
{

    internal static string FormatMarkdownHeadlines(this string mdStream)
    {
        string pattern = @"(#+)([\w]+)";
        string replacement = @"$1 $2";
        string result = Regex.Replace(mdStream, pattern, replacement);

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
        return null;
    }
}
