namespace MediaOrganizer.Services;

internal static class MediaExtensionHelper
{
    private static readonly HashSet<string> s_photoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".arw",
        ".bmp",
        ".cr2",
        ".cr3",
        ".dng",
        ".gif",
        ".heic",
        ".heif",
        ".jpeg",
        ".jpg",
        ".nef",
        ".orf",
        ".png",
        ".raf",
        ".rw2",
        ".tif",
        ".tiff",
        ".webp"
    };

    private static readonly HashSet<string> s_videoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3gp",
        ".avi",
        ".m4v",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpeg",
        ".mpg",
        ".mts",
        ".webm",
        ".wmv"
    };

    internal static bool IsPhotoExtension(string extension) => s_photoExtensions.Contains(extension);

    internal static bool IsVideoExtension(string extension) => s_videoExtensions.Contains(extension);

    internal static bool TryGetExtension(string path, out string extension)
    {
        foreach (string supportedExtension in s_photoExtensions.Concat(s_videoExtensions))
        {
            if (!string.Equals(supportedExtension, Path.GetExtension(path), StringComparison.OrdinalIgnoreCase))
                continue;

            extension = supportedExtension;

            return true;
        }

        extension = string.Empty;

        return false;
    }
}
