using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MediaOrganizer.Models;
using Spectre.Console;

namespace MediaOrganizer.Cli;

[ExcludeFromCodeCoverage]
internal static class CliRenderer
{
    internal static void RenderHeader()
    {
        AnsiConsole.Write(new FigletText("MediaOrganizer").Color(Color.Cyan1));
        AnsiConsole.MarkupLine(
            "MediaOrganizer renames photos and videos by capture time, replacing the different naming patterns created by cameras, apps, and media sources.");
        AnsiConsole.MarkupLine(
            "Your album becomes one clean sequence: [bold]0001.heic[/], [bold]0002.mov[/], [bold]0003.jpg[/], [bold]0004.mp4[/].");
        AnsiConsole.WriteLine();
    }

    internal static void RenderResult(OrganizationResult organizationResult, OrganizationOptions organizationOptions)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Organization completed.[/]");
        AnsiConsole.Write(new Table()
            .RoundedBorder()
            .Title("Summary")
            .AddColumn("Item")
            .AddColumn(new TableColumn("Total").RightAligned())
            .AddRow("Organized media", organizationResult.OrganizedMedia.ToString(CultureInfo.InvariantCulture))
            .AddRow("Processed folders", organizationResult.ProcessedDirectories.ToString(CultureInfo.InvariantCulture))
            .AddRow(
                "Removed duplicate media",
                organizationOptions.RemoveDuplicates
                    ? organizationResult.RemovedDuplicates.ToString(CultureInfo.InvariantCulture)
                    : "0")
            .AddRow(
                "Removed subfolders",
                organizationOptions.FlattenDirectory
                    ? organizationResult.RemovedSubdirectories.ToString(CultureInfo.InvariantCulture)
                    : "0"));
    }

    internal static void RenderWorkspace(
        string directoryPath, DirectoryAnalysis directoryAnalysis, OrganizationOptions? organizationOptions = null)
    {
        RenderHeader();
        AnsiConsole.MarkupLine($"Folder path: [grey]{Markup.Escape(directoryPath)}[/]");
        AnsiConsole.WriteLine();
        RenderAnalysis(directoryAnalysis);

        if (organizationOptions is not null)
            RenderSelectedOptions(organizationOptions);
    }

    private static string FormatYesNo(bool value) => value ? "Yes" : "No";

    private static void RenderAnalysis(DirectoryAnalysis directoryAnalysis)
    {
        Tree tree = new("Analysis");
        TreeNode mainTreeNode =
            tree.AddNode(
                $"[bold]Total media[/]: {directoryAnalysis.TotalMedia.ToString(CultureInfo.InvariantCulture)}");
        mainTreeNode.AddNode($"Photos: {directoryAnalysis.TotalPhotos.ToString(CultureInfo.InvariantCulture)}");
        mainTreeNode.AddNode($"Videos: {directoryAnalysis.TotalVideos.ToString(CultureInfo.InvariantCulture)}");
        TreeNode duplicateTreeNode = tree.AddNode(
            $"[bold]Duplicate media[/]: {directoryAnalysis.TotalDuplicateMedia.ToString(CultureInfo.InvariantCulture)}");
        duplicateTreeNode.AddNode(
            $"Duplicate photos: {directoryAnalysis.TotalDuplicatePhotos.ToString(CultureInfo.InvariantCulture)}");
        duplicateTreeNode.AddNode(
            $"Duplicate videos: {directoryAnalysis.TotalDuplicateVideos.ToString(CultureInfo.InvariantCulture)}");
        tree.AddNode(
            $"[bold]Subfolders[/]: {directoryAnalysis.TotalSubdirectories.ToString(CultureInfo.InvariantCulture)}");
        AnsiConsole.Write(tree);
        RenderExtensionBreakdown("By extension", directoryAnalysis.GetExtensionCounts());
        RenderExtensionBreakdown("Duplicate media by extension", directoryAnalysis.GetDuplicateExtensionCounts());
    }

    private static void RenderExtensionBreakdown(string title, IList<ExtensionCount> extensionCounts)
    {
        if (extensionCounts.Count is 0)
            return;

        Table table = new Table()
            .RoundedBorder()
            .Title(title)
            .AddColumn("Extension")
            .AddColumn(new TableColumn("Total").RightAligned());

        foreach (ExtensionCount extensionCount in extensionCounts)
            table.AddRow(extensionCount.Extension, extensionCount.Count.ToString(CultureInfo.InvariantCulture));

        AnsiConsole.Write(table);
    }

    private static void RenderSelectedOptions(OrganizationOptions organizationOptions)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Selected options[/]");
        AnsiConsole.MarkupLine($"Remove duplicate media: [bold]{FormatYesNo(organizationOptions.RemoveDuplicates)}[/]");
        AnsiConsole.MarkupLine(
            $"Move subfolder contents to root: [bold]{FormatYesNo(organizationOptions.FlattenDirectory)}[/]");
        AnsiConsole.MarkupLine(
            "[yellow]This operation renames files and may delete duplicate media and subfolders. Review your backup before proceeding.[/]");
        AnsiConsole.WriteLine();
    }
}
