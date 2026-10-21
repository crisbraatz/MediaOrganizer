using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace MediaOrganizer.Services;

internal static class CaptureDateExtractor
{
    private const int ExifDateTime = 0x0132;
    private const int ExifDateTimeOriginal = 0x9003;
    private const int ExifDateTimeDigitized = 0x9004;
    private const int ExifOffsetTime = 0x9010;
    private const int ExifOffsetTimeOriginal = 0x9011;
    private const int ExifOffsetTimeDigitized = 0x9012;
    private const int ExifSubSecTime = 0x9290;
    private const int ExifSubSecTimeOriginal = 0x9291;
    private const int ExifSubSecTimeDigitized = 0x9292;
    private const int QuickTimeCreated = 0x0003;
    private const int QuickTimeMetadataCreationDate = 0x0007;

    internal static DateTimeOffset Extract(string filePath)
    {
        try
        {
            return TryExtract(ImageMetadataReader.ReadMetadata(filePath)) ?? GetFileSystemDate(filePath);
        }
        catch (Exception e) when (e is ImageProcessingException or IOException or UnauthorizedAccessException)
        {
            return GetFileSystemDate(filePath);
        }
    }

    private static DateTimeOffset? GetDateTime(object? value) => value switch
    {
        DateTime { Kind: DateTimeKind.Utc } dateTime => new DateTimeOffset(dateTime),
        DateTime { Kind: DateTimeKind.Local } dateTime => new DateTimeOffset(dateTime),
        DateTime dateTime => new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime)),
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        _ => null
    };

    private static DateTimeOffset? GetExifDate(
        MetadataExtractor.Directory directory, int dateTag, int offsetTag, int subSecondTag)
    {
        if (!DateTime.TryParseExact(
                directory.GetObject(dateTag)?.ToString(),
                ["yyyy:MM:dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dateTime))
            return null;

        string? digits = directory.GetObject(subSecondTag)?.ToString()?.Trim();

        if (!string.IsNullOrEmpty(digits) && digits.All(char.IsAsciiDigit))
        {
            string ticks = digits.Length >= 7 ? digits[..7] : digits.PadRight(7, '0');

            if (long.TryParse(ticks, CultureInfo.InvariantCulture, out long fractionalTicks))
                dateTime = dateTime.AddTicks(fractionalTicks);
        }

        TimeSpan? offset = null;
        string? text = directory.GetObject(offsetTag)?.ToString()?.Trim();

        if (!string.IsNullOrEmpty(text) &&
            text is ['+' or '-', _, _, ':', _, _] &&
            int.TryParse(text.AsSpan(1, 2), CultureInfo.InvariantCulture, out int hours) &&
            int.TryParse(text.AsSpan(4, 2), CultureInfo.InvariantCulture, out int minutes) &&
            hours <= 14 &&
            minutes <= 59)
        {
            offset = new TimeSpan(hours, minutes, 0);

            if (text[0] == '-')
                offset = -offset;
        }

        return new DateTimeOffset(dateTime, offset ?? TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    private static DateTimeOffset GetFileSystemDate(string filePath)
    {
        FileInfo fileInfo = new(filePath);

        return new DateTimeOffset(
            fileInfo.CreationTimeUtc == DateTime.MinValue ? fileInfo.LastWriteTimeUtc : fileInfo.CreationTimeUtc,
            TimeSpan.Zero);
    }

    private static DateTimeOffset? TryExtract(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        foreach (ExifSubIfdDirectory directory in directories.OfType<ExifSubIfdDirectory>())
        {
            DateTimeOffset? original =
                GetExifDate(directory, ExifDateTimeOriginal, ExifOffsetTimeOriginal, ExifSubSecTimeOriginal);

            if (original is not null)
                return original;

            DateTimeOffset? digitized =
                GetExifDate(directory, ExifDateTimeDigitized, ExifOffsetTimeDigitized, ExifSubSecTimeDigitized);

            if (digitized is not null)
                return digitized;
        }

        foreach (ExifIfd0Directory directory in directories.OfType<ExifIfd0Directory>())
        {
            DateTimeOffset? dateTime = GetExifDate(directory, ExifDateTime, ExifOffsetTime, ExifSubSecTime);

            if (dateTime is not null)
                return dateTime;
        }

        foreach (QuickTimeMetadataHeaderDirectory directory in directories.OfType<QuickTimeMetadataHeaderDirectory>())
        {
            DateTimeOffset? creationDate = GetDateTime(directory.GetObject(QuickTimeMetadataCreationDate));

            if (creationDate is not null)
                return creationDate;
        }

        foreach (QuickTimeMovieHeaderDirectory directory in directories.OfType<QuickTimeMovieHeaderDirectory>())
        {
            DateTimeOffset? created = GetDateTime(directory.GetObject(QuickTimeCreated));

            if (created is not null)
                return created;
        }

        return null;
    }
}
