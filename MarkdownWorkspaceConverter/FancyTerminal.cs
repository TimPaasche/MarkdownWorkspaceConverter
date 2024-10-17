using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownWorkspaceConverter;
public static class FancyTerminal
{

    private const char _borderChar = ' ';
    private const int _borderLength = 110;

    public static void PrintHeader(string task, string location)
    {
        location = " [ " + location + " ]";
        task = $"{task} ";
        int countOfDashes = _borderLength > location.Length
            ? _borderLength - location.Length
            : 0;

        Console.WriteLine($"{task.PadRight(countOfDashes, _borderChar)}{location}");
        Console.WriteLine(new string('-', _borderLength));
    }

    public static void PrintSuccess(string task, string location)
    {
        location = " [ " + location + " ]";
        task = $"{task} ";
        int countOfDashes = _borderLength > location.Length
            ? _borderLength - location.Length
            : 0;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{task.PadRight(countOfDashes, _borderChar)}{location}");
        Console.ResetColor();
    }

    public static void PrintInfo(string task, string location)
    {
        location = " [ " + location + " ]";
        task = $"{task} ";
        int countOfDashes = _borderLength > location.Length
            ? _borderLength - location.Length
            : 0;

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{task.PadRight(countOfDashes, _borderChar)}{location}");
        Console.ResetColor();
    }

    public static void PrintWarning(string task, string location)
    {
        location = " [ " + location + " ]";
        task = $"{task} ";
        int countOfDashes = _borderLength > location.Length
            ? _borderLength - location.Length
            : 0;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{task.PadRight(countOfDashes, _borderChar)}{location}");
        Console.ResetColor();
    }

    public static void PrintError(string task, string location)
    {
        location = " [ " + location + " ]";
        task = $"{task} ";
        int countOfDashes = _borderLength > location.Length
            ? _borderLength - location.Length
            : 0;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{task.PadRight(countOfDashes, _borderChar)}{location}");
        Console.ResetColor();
    }
}
