using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = System.IO.Directory;

const string path = "/Users/user/Library/Mobile Documents/com~apple~CloudDocs/";

var fileInfoWithDateTimeList = new List<(FileInfo File, DateTime DateTaken)>();

foreach (var file in Directory.EnumerateFiles(path))
{
    var fileInfo = new FileInfo(file);
    if (!new[] { ".heic", ".jpg", ".jpeg", ".mov", ".mp4", ".png" }.Contains(fileInfo.Extension.ToLower()))
        continue;

    fileInfoWithDateTimeList.Add((fileInfo, GetDateTaken(fileInfo)));
}

var index = 1;

foreach (var (fileInfo, _) in fileInfoWithDateTimeList.OrderBy(x => x.DateTaken))
{
    var file = Path.Combine(path, $"{index:D4}{fileInfo.Extension.ToLower()}");
    if (!File.Exists(file))
        fileInfo.MoveTo(file);

    index++;
}

return;

static DateTime GetDateTaken(FileInfo fileInfo)
{
    try
    {
        if (fileInfo.Extension.Equals(".mp4", StringComparison.CurrentCultureIgnoreCase) ||
            fileInfo.Extension.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
            return fileInfo.CreationTime;

        return DateTime.TryParseExact(ImageMetadataReader
                .ReadMetadata(fileInfo.FullName)
                .OfType<ExifSubIfdDirectory>()
                .FirstOrDefault()
                ?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal),
            "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime)
            ? dateTime
            : fileInfo.CreationTime;
    }
    catch
    {
        return fileInfo.CreationTime;
    }
}