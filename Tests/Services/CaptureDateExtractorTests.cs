using MediaOrganizer.Services;

namespace Tests.Services;

public sealed class CaptureDateExtractorTests : IDisposable
{
    private readonly string _rootPath =
        Path.Combine(Path.GetTempPath(), $"mediaorganizer-date-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ShouldUseDateTimeOriginalWhenMetadataExists()
    {
        DateTimeOffset extractedCaptureDate =
            CaptureDateExtractor.Extract(TestMediaFactory.CreateJpg(_rootPath, "photo.jpg", "2022:03:04 05:06:07"));

        Assert.Equal(2022, extractedCaptureDate.Year);
        Assert.Equal(3, extractedCaptureDate.Month);
        Assert.Equal(4, extractedCaptureDate.Day);
        Assert.Equal(5, extractedCaptureDate.Hour);
        Assert.Equal(6, extractedCaptureDate.Minute);
        Assert.Equal(7, extractedCaptureDate.Second);
    }

    [Fact]
    public void ShouldUseDashedDateTimeOriginalWhenMetadataExists()
    {
        DateTimeOffset extractedCaptureDate =
            CaptureDateExtractor.Extract(TestMediaFactory.CreateJpg(_rootPath, "photo.jpg", "2022-03-04 05:06:07"));

        Assert.Equal(2022, extractedCaptureDate.Year);
        Assert.Equal(3, extractedCaptureDate.Month);
        Assert.Equal(4, extractedCaptureDate.Day);
        Assert.Equal(5, extractedCaptureDate.Hour);
        Assert.Equal(6, extractedCaptureDate.Minute);
        Assert.Equal(7, extractedCaptureDate.Second);
    }

    [Fact]
    public void ShouldFallBackToFileCreationTimeWhenMetadataCannotBeRead()
    {
        DateTimeOffset createdAt = new(2020, 6, 1, 14, 30, 0, TimeSpan.Zero);
        string path = TestMediaFactory.CreateVideo(_rootPath, "clip.mp4", 42);
        TestMediaFactory.SetCreatedAt(path, createdAt);

        DateTimeOffset extractedCaptureDate = CaptureDateExtractor.Extract(path);

        Assert.Equal(createdAt.UtcDateTime, extractedCaptureDate.UtcDateTime);
    }

    [Fact]
    public void ShouldFallBackToFileCreationTimeWhenMetadataDateCannotBeParsed()
    {
        DateTimeOffset createdAt = new(2020, 6, 1, 14, 30, 0, TimeSpan.Zero);
        string path = TestMediaFactory.CreateJpg(_rootPath, "photo.jpg", "not a date value");
        TestMediaFactory.SetCreatedAt(path, createdAt);

        DateTimeOffset extractedCaptureDate = CaptureDateExtractor.Extract(path);

        Assert.Equal(createdAt.UtcDateTime, extractedCaptureDate.UtcDateTime);
    }

    [Fact]
    public void ShouldPrioritizeExifOriginalAndPreserveItsOffsetAndSubSeconds()
    {
        Assert.Equal(
            new DateTimeOffset(2026, 8, 18, 11, 0, 26, TimeSpan.FromHours(2)).AddMilliseconds(621),
            CaptureDateExtractor.Extract(TestMediaFactory.CreateJpg(
                _rootPath, "photo.jpg", "2026:08:18 11:00:26", "2026:08:18 10:00:00", "+02:00", "621")));
    }

    [Fact]
    public void ShouldPrioritizeQuickTimeContentCreationDateOverMovieHeaderDate()
    {
        Assert.Equal(new DateTimeOffset(2016, 1, 1, 5, 9, 38, TimeSpan.Zero),
            CaptureDateExtractor.Extract(TestMediaFactory.CreateQuickTime(
                _rootPath,
                "clip.mov",
                new DateTimeOffset(2026, 9, 6, 22, 10, 9, TimeSpan.Zero),
                "2016-01-01T05:09:38+00:00")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, true);
    }
}
