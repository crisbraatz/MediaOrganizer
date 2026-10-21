using System.Diagnostics.CodeAnalysis;
using System.Text;
using Spectre.Console;

namespace MediaOrganizer.Cli;

internal static class CliPrompts
{
    internal static void AppendCommandKey(StringBuilder stringBuilder, ConsoleKeyInfo consoleKeyInfo)
    {
        ArgumentNullException.ThrowIfNull(stringBuilder);

        if (consoleKeyInfo.Key is ConsoleKey.Backspace)
        {
            if (stringBuilder.Length > 0)
                stringBuilder.Length--;

            return;
        }

        if (!char.IsControl(consoleKeyInfo.KeyChar))
            stringBuilder.Append(consoleKeyInfo.KeyChar);
    }

    [ExcludeFromCodeCoverage]
    internal static bool AskYesNo(string title, Action action)
    {
        string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title($"{title} [grey](Use arrow keys and Enter.)[/]")
            .PageSize(4)
            .AddChoices("Yes", "No", "Restart", "Exit"));
        CliCommandHandler.Handle(choice);
        AnsiConsole.Clear();
        action();

        return string.Equals(choice, "Yes", StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    internal static void ReadCommand(StringBuilder stringBuilder)
    {
        ArgumentNullException.ThrowIfNull(stringBuilder);

        if (Console.IsInputRedirected || !Console.KeyAvailable)
            return;

        ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(intercept: true);

        if (consoleKeyInfo.Key is ConsoleKey.Enter)
        {
            CliCommandHandler.Handle(stringBuilder.ToString());
            stringBuilder.Clear();

            return;
        }

        AppendCommandKey(stringBuilder, consoleKeyInfo);
    }

    [ExcludeFromCodeCoverage]
    internal static bool WaitForRestartOrClose()
    {
        if (Console.IsInputRedirected)
            return false;

        AnsiConsole.MarkupLine("[grey]Type restart to start over, or exit to close MediaOrganizer.[/]");

        while (true)
        {
            Console.Write(CliText.s_prompt);
            string command = Console.ReadLine() ?? string.Empty;

            switch (CliCommandHandler.Parse(command))
            {
                case CliCommand.Restart:
                    return true;
                case CliCommand.Exit:
                    Environment.Exit(0);

                    return false;
                case CliCommand.None:
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported CLI command: {command}.");
            }
        }
    }
}
