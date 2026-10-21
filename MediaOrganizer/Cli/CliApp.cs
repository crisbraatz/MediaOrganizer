using System.Diagnostics.CodeAnalysis;
using System.Text;
using MediaOrganizer.Models;
using MediaOrganizer.Services;
using Spectre.Console;

namespace MediaOrganizer.Cli;

[ExcludeFromCodeCoverage]
internal static class CliApp
{
    internal static void Run()
    {
        while (true)
        {
            try
            {
                string directoryPath = ReadExistingDirectoryPath();
                DirectoryAnalysis directoryAnalysis = AnalyzeDirectory(directoryPath);
                AnsiConsole.Clear();
                CliRenderer.RenderWorkspace(directoryPath, directoryAnalysis);
                OrganizationOptions organizationOptions = new(
                    CliPrompts.AskYesNo(
                        "Remove duplicate media?", () => CliRenderer.RenderWorkspace(directoryPath, directoryAnalysis)),
                    CliPrompts.AskYesNo(
                        "Move everything from subfolders to the root folder?",
                        () => CliRenderer.RenderWorkspace(directoryPath, directoryAnalysis)));
                AnsiConsole.Clear();
                CliRenderer.RenderWorkspace(directoryPath, directoryAnalysis, organizationOptions);

                if (!CliPrompts.AskYesNo(
                        "Proceed with these irreversible changes?",
                        () => CliRenderer.RenderWorkspace(directoryPath, directoryAnalysis, organizationOptions)))
                    throw new OperationCanceledException(CliCommandHandler.RestartMessage);

                CliRenderer.RenderResult(AnsiConsole
                    .Progress()
                    .AutoClear(true)
                    .Columns(new ProgressBarColumn(), new PercentageColumn())
                    .Start(x =>
                    {
                        ProgressTask progressTask =
                            x.AddTask("Organizing media", maxValue: Math.Max(1, directoryAnalysis.TotalMedia * 2));

                        return MediaOrganizerService.Organize(
                            directoryAnalysis,
                            organizationOptions,
                            new Progress<OrganizationProgress>(y =>
                            {
                                progressTask.MaxValue = Math.Max(1, y.Total);
                                progressTask.Value = Math.Min(y.Completed, progressTask.MaxValue);
                            }));
                    }), organizationOptions);

                if (CliPrompts.WaitForRestartOrClose())
                {
                    AnsiConsole.Clear();

                    continue;
                }

                return;
            }
            catch (OperationCanceledException e) when (e.Message is CliCommandHandler.RestartMessage)
            {
                AnsiConsole.Clear();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                AnsiConsole.Clear();
                CliRenderer.RenderHeader();
                AnsiConsole.MarkupLine(
                    $"[red]MediaOrganizer could not complete this operation: {Markup.Escape(e.Message)}[/]");
                AnsiConsole.WriteLine();

                if (CliPrompts.WaitForRestartOrClose())
                {
                    AnsiConsole.Clear();

                    continue;
                }

                return;
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                AnsiConsole.Clear();
                CliRenderer.RenderHeader();
                AnsiConsole.MarkupLine(
                    $"[red]MediaOrganizer stopped because something unexpected happened: {Markup.Escape(e.Message)}[/]");
                AnsiConsole.WriteLine();

                if (CliPrompts.WaitForRestartOrClose())
                {
                    AnsiConsole.Clear();

                    continue;
                }

                return;
            }
        }
    }

    private static DirectoryAnalysis AnalyzeDirectory(string directoryPath)
    {
        Task<DirectoryAnalysis> directoryAnalysisTask = Task.Run(() => DirectoryAnalyzer.Analyze(directoryPath));
        string[] frames = ["-", "\\", "|", "/"];
        int frame = 0;

        while (!directoryAnalysisTask.IsCompleted)
        {
            CliPrompts.ReadCommand(new StringBuilder());
            Console.Write($"\rAnalyzing media... {frames[frame]}   Type restart to start over, or exit to close.");
            frame = (frame + 1) % frames.Length;
            Thread.Sleep(100);
        }

        Console.Write('\r');
        Console.Write(new string(' ', (Console.WindowWidth > 0 ? Console.WindowWidth : 120) - 1));
        Console.Write('\r');

        return directoryAnalysisTask.GetAwaiter().GetResult();
    }

    private static string ReadExistingDirectoryPath()
    {
        string? errorMessage = null;

        while (true)
        {
            AnsiConsole.Clear();
            CliRenderer.RenderHeader();

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(errorMessage)}[/]");
                AnsiConsole.WriteLine();
            }

            string directoryPath = CliDirectoryReader.Read();

            if (Directory.Exists(directoryPath))
                return Path.GetFullPath(directoryPath);

            errorMessage =
                $"Folder not found: {directoryPath}. Enter a valid absolute folder path, type restart, or type exit.";
        }
    }
}
