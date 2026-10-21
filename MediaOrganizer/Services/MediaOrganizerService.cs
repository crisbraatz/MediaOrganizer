using System.Globalization;
using MediaOrganizer.Models;

namespace MediaOrganizer.Services;

internal static class MediaOrganizerService
{
    internal static OrganizationResult Organize(
        DirectoryAnalysis directoryAnalysis,
        OrganizationOptions organizationOptions,
        IProgress<OrganizationProgress>? organizationProgress = null)
    {
        ArgumentNullException.ThrowIfNull(directoryAnalysis);
        ArgumentNullException.ThrowIfNull(organizationOptions);

        HashSet<string> duplicatePaths = organizationOptions.RemoveDuplicates
            ? directoryAnalysis.DuplicateGroups
                .SelectMany(x => x.Duplicates)
                .Select(x => x.Path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int removedDuplicates = 0;

        foreach (string duplicatePath in duplicatePaths.Where(File.Exists))
        {
            File.Delete(duplicatePath);
            removedDuplicates++;
        }

        List<MediaFile> mediaFiles = [.. directoryAnalysis.MediaFiles.Where(x => !duplicatePaths.Contains(x.Path))];

        return organizationOptions.FlattenDirectory
            ? OrganizeRoot(directoryAnalysis, mediaFiles, removedDuplicates, organizationProgress)
            : OrganizePerDirectory(mediaFiles, removedDuplicates, organizationProgress);
    }

    private static OrganizationResult OrganizePerDirectory(
        List<MediaFile> mediaFiles, int removedDuplicates, IProgress<OrganizationProgress>? organizationProgress)
    {
        List<IGrouping<string, MediaFile>> directoryGroups =
        [
            .. mediaFiles
                .GroupBy(x => Directory.GetParent(x.Path)?.FullName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        ];
        int completed = 0;
        int organized = 0;
        int totalOperations = mediaFiles.Count * 2;

        foreach (IGrouping<string, MediaFile> directoryGroup in directoryGroups)
        {
            List<StagedMediaFile> stagedMediaFiles = StageMediaFiles(
                [.. directoryGroup], directoryGroup.Key, organizationProgress, completed, totalOperations);
            RenameSequentially(
                stagedMediaFiles,
                directoryGroup.Key,
                organizationProgress,
                completed + stagedMediaFiles.Count,
                totalOperations);
            completed += stagedMediaFiles.Count * 2;
            organized += stagedMediaFiles.Count;
        }

        return new OrganizationResult(organized, directoryGroups.Count, removedDuplicates, 0);
    }

    private static OrganizationResult OrganizeRoot(
        DirectoryAnalysis directoryAnalysis,
        List<MediaFile> mediaFiles,
        int removedDuplicates,
        IProgress<OrganizationProgress>? organizationProgress)
    {
        int totalOperations = mediaFiles.Count * 2;
        List<StagedMediaFile> stagedMediaFiles =
            StageMediaFiles(mediaFiles, directoryAnalysis.RootPath, organizationProgress, 0, totalOperations);

        foreach (string subdirectory in directoryAnalysis.Subdirectories
                     .Where(Directory.Exists).OrderByDescending(x => x.Length))
        {
            foreach (string file in Directory.GetFiles(subdirectory, "*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file);
                string path = Path.Combine(directoryAnalysis.RootPath, fileName);
                int index = 1;

                while (File.Exists(path))
                {
                    path = Path.Combine(
                        directoryAnalysis.RootPath,
                        string.Concat(
                            Path.GetFileNameWithoutExtension(fileName),
                            "-",
                            index.ToString(CultureInfo.InvariantCulture),
                            Path.GetExtension(fileName)));

                    index++;
                }

                File.Move(file, path);
            }
        }

        foreach (string subdirectory in directoryAnalysis.Subdirectories
                     .Where(Directory.Exists).OrderByDescending(x => x.Length))
            Directory.Delete(subdirectory, true);

        RenameSequentially(
            stagedMediaFiles,
            directoryAnalysis.RootPath,
            organizationProgress,
            stagedMediaFiles.Count,
            totalOperations);

        return new OrganizationResult(
            stagedMediaFiles.Count, 1, removedDuplicates, directoryAnalysis.TotalSubdirectories);
    }

    private static void RenameSequentially(
        List<StagedMediaFile> stagedMediaFiles,
        string targetDirectory,
        IProgress<OrganizationProgress>? organizationProgress,
        int completedOffset,
        int total)
    {
        int completed = completedOffset;

        for (int index = 0; index < stagedMediaFiles.Count; index++)
        {
            string path = Path.Combine(targetDirectory, string.Concat(
                (index + 1).ToString(
                    $"D{Math.Max(1, stagedMediaFiles.Count.ToString(CultureInfo.InvariantCulture).Length)}",
                    CultureInfo.InvariantCulture),
                stagedMediaFiles[index].Extension));

            if (File.Exists(path))
                throw new IOException($"Could not create '{path}' because a media file with that name already exists.");

            File.Move(stagedMediaFiles[index].Path, path);
            completed++;
            organizationProgress?.Report(new OrganizationProgress(completed, total));
        }
    }

    private static List<StagedMediaFile> StageMediaFiles(
        List<MediaFile> mediaFiles,
        string targetDirectory,
        IProgress<OrganizationProgress>? organizationProgress,
        int completedOffset = 0,
        int? totalOverride = null)
    {
        List<StagedMediaFile> stagedMediaFiles = [];
        int completed = completedOffset;

        foreach (MediaFile mediaFile in mediaFiles
                     .OrderBy(x => x.CapturedAt).ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
        {
            string path = Path.Combine(targetDirectory, $".mediaorganizer-{Guid.NewGuid():N}{mediaFile.Extension}");
            File.Move(mediaFile.Path, path);
            stagedMediaFiles.Add(new StagedMediaFile(path, mediaFile.Extension));
            completed++;
            organizationProgress?.Report(new OrganizationProgress(completed, totalOverride ?? mediaFiles.Count));
        }

        return stagedMediaFiles;
    }
}
