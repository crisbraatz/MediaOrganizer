using MediaOrganizer.Services;

namespace MediaOrganizer.Models;

internal sealed class DirectoryAnalysis(
    string rootPath,
    IList<MediaFile> mediaFiles,
    IList<DuplicateGroup> duplicateGroups,
    IList<string> subdirectories)
{
    internal string RootPath { get; } = rootPath;
    internal IList<MediaFile> MediaFiles { get; } = mediaFiles;
    internal IList<DuplicateGroup> DuplicateGroups { get; } = duplicateGroups;
    internal IList<string> Subdirectories { get; } = subdirectories;
    internal int TotalMedia => MediaFiles.Count;
    internal int TotalPhotos => MediaFiles.Count(x => MediaExtensionHelper.IsPhotoExtension(x.Extension));
    internal int TotalVideos => MediaFiles.Count(x => MediaExtensionHelper.IsVideoExtension(x.Extension));
    internal int TotalDuplicateMedia => DuplicateGroups.Sum(x => x.Duplicates.Count);

    internal int TotalDuplicatePhotos =>
        DuplicateGroups.SelectMany(x => x.Duplicates).Count(x => MediaExtensionHelper.IsPhotoExtension(x.Extension));

    internal int TotalDuplicateVideos =>
        DuplicateGroups.SelectMany(x => x.Duplicates).Count(x => MediaExtensionHelper.IsVideoExtension(x.Extension));

    internal int TotalSubdirectories => Subdirectories.Count;

    internal IList<ExtensionCount> GetExtensionCounts() => MediaFiles
        .GroupBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        .Select(x => new ExtensionCount(x.Key, x.Count()))
        .ToList();

    internal IList<ExtensionCount> GetDuplicateExtensionCounts() => DuplicateGroups
        .SelectMany(x => x.Duplicates)
        .GroupBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        .Select(x => new ExtensionCount(x.Key, x.Count()))
        .ToList();
}
