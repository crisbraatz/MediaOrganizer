using MediaOrganizer.Models;
using MediaOrganizer.Services;

namespace Tests.Services;

public sealed class DirectoryAnalyzerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"mediaorganizer-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ShouldCountMediaByTypeExtensionDuplicatesAndSubdirectories()
    {
        string first = TestMediaFactory.CreateWhiteJpg(_rootPath, "IMG_1001.JPG");
        string second = TestMediaFactory.CreateBlueJpg(_rootPath, "IMG_1002.jpeg");
        string duplicate = TestMediaFactory.CreateWhiteJpg(_rootPath, "copy.jpg");
        string videoDirectory = Path.Combine(_rootPath, "gopro");
        _ = TestMediaFactory.CreateVideo(videoDirectory, "GX010001.MP4", 1);
        _ = TestMediaFactory.CreateVideo(videoDirectory, "GX010002.MOV", 2);
        TestMediaFactory.SetCreatedAt(first, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(second, new DateTimeOffset(2024, 1, 1, 10, 1, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(duplicate, new DateTimeOffset(2024, 1, 1, 10, 2, 0, TimeSpan.Zero));

        DirectoryAnalysis directoryAnalysis = DirectoryAnalyzer.Analyze(_rootPath);

        Assert.Equal(5, directoryAnalysis.TotalMedia);
        Assert.Equal(3, directoryAnalysis.TotalPhotos);
        Assert.Equal(2, directoryAnalysis.TotalVideos);
        Assert.Equal(1, directoryAnalysis.TotalDuplicateMedia);
        Assert.Equal(1, directoryAnalysis.TotalDuplicatePhotos);
        Assert.Equal(0, directoryAnalysis.TotalDuplicateVideos);
        Assert.Equal(1, directoryAnalysis.TotalSubdirectories);
        Assert.Contains(directoryAnalysis.GetExtensionCounts(), x => x is { Extension: ".jpg", Count: 2 });
        Assert.Contains(directoryAnalysis.GetExtensionCounts(), x => x is { Extension: ".mp4", Count: 1 });
        Assert.Contains(directoryAnalysis.GetDuplicateExtensionCounts(), x => x is { Extension: ".jpg", Count: 1 });
    }

    [Fact]
    public void ShouldKeepOldestDuplicateAndMarkNewerFilesAsDuplicates()
    {
        string newer = TestMediaFactory.CreateWhiteJpg(_rootPath, "newer.jpg");
        string oldest = TestMediaFactory.CreateWhiteJpg(_rootPath, "oldest.jpg");
        string newest = TestMediaFactory.CreateWhiteJpg(_rootPath, "newest.jpg");
        TestMediaFactory.SetCreatedAt(newer, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(oldest, new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(newest, new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero));

        DirectoryAnalysis directoryAnalysis = DirectoryAnalyzer.Analyze(_rootPath);

        DuplicateGroup duplicateGroup = Assert.Single(directoryAnalysis.DuplicateGroups);
        Assert.Equal(oldest, duplicateGroup.Keeper.Path);
        Assert.Equal(duplicateGroup.Keeper.Hash, duplicateGroup.Hash);
        Assert.Equal([newer, newest], duplicateGroup.Duplicates.Select(static x => x.Path));
        Assert.All(duplicateGroup.Duplicates, x => Assert.Equal(duplicateGroup.Hash, x.Hash));
    }

    [Fact]
    public void ShouldUsePathAsDuplicateTieBreakerWhenCaptureDatesMatch()
    {
        string second = TestMediaFactory.CreateWhiteJpg(_rootPath, "b-copy.jpg");
        string first = TestMediaFactory.CreateWhiteJpg(_rootPath, "a-original.jpg");
        DateTimeOffset capturedAt = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        TestMediaFactory.SetCreatedAt(second, capturedAt);
        TestMediaFactory.SetCreatedAt(first, capturedAt);

        DirectoryAnalysis directoryAnalysis = DirectoryAnalyzer.Analyze(_rootPath);

        DuplicateGroup duplicateGroup = Assert.Single(directoryAnalysis.DuplicateGroups);
        Assert.Equal(first, duplicateGroup.Keeper.Path);
        MediaFile duplicate = Assert.Single(duplicateGroup.Duplicates);
        Assert.Equal(second, duplicate.Path);
    }

    [Fact]
    public void ShouldIgnoreUnsupportedFiles()
    {
        _ = TestMediaFactory.CreateWhiteJpg(_rootPath, "photo.jpg");
        File.WriteAllText(Path.Combine(_rootPath, "notes.txt"), "not media");

        DirectoryAnalysis directoryAnalysis = DirectoryAnalyzer.Analyze(_rootPath);

        Assert.Equal(1, directoryAnalysis.TotalMedia);
        Assert.Single(directoryAnalysis.MediaFiles);
    }

    [Fact]
    public void ShouldThrowWhenDirectoryDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing");

        DirectoryNotFoundException e =
            Assert.Throws<DirectoryNotFoundException>(() => DirectoryAnalyzer.Analyze(missingPath));

        Assert.Contains(missingPath, e.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, true);
    }
}
