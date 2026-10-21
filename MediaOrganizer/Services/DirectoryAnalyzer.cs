using System.Security.Cryptography;
using MediaOrganizer.Models;

namespace MediaOrganizer.Services;

internal static class DirectoryAnalyzer
{
    internal static DirectoryAnalysis Analyze(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"The folder '{rootPath}' was not found.");

        string fullRootPath = Path.GetFullPath(rootPath);
        string[] files = Directory.GetFiles(fullRootPath, "*", SearchOption.AllDirectories);
        List<MediaFile> mediaFiles = [];

        foreach (string file in files)
        {
            if (!MediaExtensionHelper.TryGetExtension(file, out string extension))
                continue;

            mediaFiles.Add(new MediaFile(file, extension, CaptureDateExtractor.Extract(file), ComputeHash(file)));
        }

        return new DirectoryAnalysis(
            fullRootPath,
            mediaFiles,
            [
                .. mediaFiles
                    .GroupBy(x => x.Hash, StringComparer.Ordinal)
                    .Where(x => x.Count() > 1)
                    .Select(x =>
                    {
                        List<MediaFile> orderedMediaFiles =
                            [.. x.OrderBy(y => y.CapturedAt).ThenBy(y => y.Path, StringComparer.OrdinalIgnoreCase)];

                        return new DuplicateGroup(x.Key, orderedMediaFiles[0], orderedMediaFiles.Skip(1).ToList());
                    })
            ],
            Directory.GetDirectories(fullRootPath, "*", SearchOption.AllDirectories));
    }

    private static string ComputeHash(string file)
    {
        using FileStream fileStream = File.OpenRead(file);

        return Convert.ToHexString(SHA256.HashData(fileStream));
    }
}
