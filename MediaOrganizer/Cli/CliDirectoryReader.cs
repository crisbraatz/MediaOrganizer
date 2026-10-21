using System.Diagnostics.CodeAnalysis;
using System.Text;
using MediaOrganizer.Models;
using MediaOrganizer.Services;
using Spectre.Console;

namespace MediaOrganizer.Cli;

[ExcludeFromCodeCoverage]
internal static class CliDirectoryReader
{
    internal static string Read()
    {
        AnsiConsole.MarkupLine(
            $"Folder path example: [grey]{Markup.Escape(OperatingSystem.IsWindows()
                ? @"C:\Users\your-user\Pictures\Trip"
                : "/Users/your-user/Pictures/Trip")}[/]");
        AnsiConsole.MarkupLine(
            "[grey]Type a path and press Tab to autocomplete folders from the current terminal location. Type restart to start over, or exit to close.[/]");

        if (Console.IsInputRedirected)
        {
            string redirectedPath = Console.ReadLine() ?? string.Empty;
            CliCommandHandler.Handle(redirectedPath);

            return redirectedPath;
        }

        StringBuilder stringBuilder = new();
        Console.Write(CliText.s_prompt);

        while (true)
        {
            ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(intercept: true);

            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    string path = stringBuilder.ToString();
                    CliCommandHandler.Handle(path);

                    return path;
                case ConsoleKey.Backspace:
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Length--;
                        RedrawInputLine(CliText.s_prompt, stringBuilder.ToString());
                    }

                    continue;
                case ConsoleKey.Tab:
                    string beforeCompletion = stringBuilder.ToString();
                    DirectoryCompletion directoryCompletion =
                        DirectoryCompleter.Complete(beforeCompletion, Environment.CurrentDirectory);

                    if (!string.Equals(beforeCompletion, directoryCompletion.Value, StringComparison.Ordinal))
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(directoryCompletion.Value);
                        RedrawInputLine(CliText.s_prompt, stringBuilder.ToString());

                        continue;
                    }

                    if (directoryCompletion.Suggestions.Count > 1)
                    {
                        Console.WriteLine();
                        AnsiConsole.MarkupLine(
                            $"[grey]{Markup.Escape(string.Join("  ", directoryCompletion.Suggestions))}[/]");
                        Console.Write(CliText.s_prompt);
                        Console.Write(stringBuilder);
                    }

                    continue;
            }

            if (!char.IsControl(consoleKeyInfo.KeyChar))
            {
                stringBuilder.Append(consoleKeyInfo.KeyChar);
                Console.Write(consoleKeyInfo.KeyChar);
            }
        }
    }

    private static void RedrawInputLine(string prompt, string value)
    {
        Console.Write('\r');
        Console.Write(new string(' ', (Console.WindowWidth > 0 ? Console.WindowWidth : 120) - 1));
        Console.Write('\r');
        Console.Write(prompt);
        Console.Write(value);
    }
}
