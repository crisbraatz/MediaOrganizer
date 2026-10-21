using MediaOrganizer.Models;
using MediaOrganizer.Services;

namespace Tests.Services;

public sealed class MediaOrganizerServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"mediaorganizer-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ShouldMoveSubdirectoryMediaToRootRemoveDuplicatesAndOrderByCaptureDate()
    {
        string photo = TestMediaFactory.CreateWhiteJpg(_rootPath, "IMG_2002.JPG");
        string duplicate = TestMediaFactory.CreateWhiteJpg(_rootPath, "renamed-copy.jpg");
        string subdirectory = Path.Combine(_rootPath, "camera");
        string video = TestMediaFactory.CreateVideo(subdirectory, "GX010001.MP4", 9);
        string note = Path.Combine(subdirectory, "notes.txt");
        File.WriteAllText(note, "keep me");
        TestMediaFactory.SetCreatedAt(video, new DateTimeOffset(2023, 5, 1, 8, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(photo, new DateTimeOffset(2023, 5, 1, 9, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(duplicate, new DateTimeOffset(2023, 5, 1, 10, 0, 0, TimeSpan.Zero));

        OrganizationResult organizationResult =
            MediaOrganizerService.Organize(DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(true, true));

        Assert.Equal(2, organizationResult.OrganizedMedia);
        Assert.Equal(1, organizationResult.RemovedDuplicates);
        Assert.Equal(1, organizationResult.RemovedSubdirectories);
        Assert.Equal(1, organizationResult.ProcessedDirectories);
        Assert.True(File.Exists(Path.Combine(_rootPath, "1.mp4")));
        Assert.True(File.Exists(Path.Combine(_rootPath, "2.jpg")));
        Assert.True(File.Exists(Path.Combine(_rootPath, "notes.txt")));
        Assert.False(Directory.Exists(subdirectory));
        Assert.False(File.Exists(duplicate));
    }

    [Fact]
    public void ShouldKeepSubdirectoriesAndUseDirectoryLocalNumbering()
    {
        string rootPhoto = TestMediaFactory.CreateBlueJpg(_rootPath, "IMG_3001.JPG");
        string subdirectory = Path.Combine(_rootPath, "android");
        string nestedPhoto = TestMediaFactory.CreateWhiteJpg(subdirectory, "PXL_3002.PNG");
        TestMediaFactory.SetCreatedAt(rootPhoto, new DateTimeOffset(2022, 2, 2, 12, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(nestedPhoto, new DateTimeOffset(2022, 2, 2, 11, 0, 0, TimeSpan.Zero));

        OrganizationResult organizationResult =
            MediaOrganizerService.Organize(DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(false, false));

        Assert.Equal(2, organizationResult.OrganizedMedia);
        Assert.Equal(2, organizationResult.ProcessedDirectories);
        Assert.True(File.Exists(Path.Combine(_rootPath, "1.jpg")));
        Assert.True(File.Exists(Path.Combine(subdirectory, "1.png")));
        Assert.True(Directory.Exists(subdirectory));
    }

    [Fact]
    public void ShouldUseWidthBasedOnFinalDirectoryTotal()
    {
        for (int index = 0; index < 12; index++)
            TestMediaFactory.SetCreatedAt(TestMediaFactory.CreateVideo(
                _rootPath, $"clip-{index}.mp4", index), new DateTimeOffset(2021, 1, 1, 0, index, 0, TimeSpan.Zero));

        MediaOrganizerService.Organize(DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(false, false));

        Assert.True(File.Exists(Path.Combine(_rootPath, "01.mp4")));
        Assert.True(File.Exists(Path.Combine(_rootPath, "12.mp4")));
    }

    [Fact]
    public void ShouldMoveSubdirectoryNonMediaFilesWithoutOverwritingRootFiles()
    {
        string subdirectory = Path.Combine(_rootPath, "camera");
        string photo = TestMediaFactory.CreateWhiteJpg(subdirectory, "photo.jpg");
        string rootNote = Path.Combine(_rootPath, "notes.txt");
        string subdirectoryNote = Path.Combine(subdirectory, "notes.txt");
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(rootNote, "root");
        File.WriteAllText(subdirectoryNote, "subdirectory");
        TestMediaFactory.SetCreatedAt(photo, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));

        OrganizationResult organizationResult =
            MediaOrganizerService.Organize(DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(false, true));

        Assert.Equal(1, organizationResult.OrganizedMedia);
        Assert.True(File.Exists(rootNote));
        Assert.Equal("root", File.ReadAllText(rootNote));
        Assert.Equal("subdirectory", File.ReadAllText(Path.Combine(_rootPath, "notes-1.txt")));
    }

    [Fact]
    public void ShouldReportProgressForStageAndRenameOperations()
    {
        _ = TestMediaFactory.CreateBlueJpg(_rootPath, "IMG_5001.JPG");
        _ = TestMediaFactory.CreateVideo(_rootPath, "clip.mp4", 7);
        List<OrganizationProgress> organizationProgresses = [];
        RecordingProgress recordingProgress = new(organizationProgresses);

        OrganizationResult organizationResult = MediaOrganizerService.Organize(
            DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(false, false), recordingProgress);

        Assert.Equal(2, organizationResult.OrganizedMedia);
        Assert.Equal(4, organizationProgresses.Count);
        Assert.All(organizationProgresses, x => Assert.Equal(4, x.Total));
        Assert.Equal([1, 2, 3, 4], organizationProgresses.Select(static x => x.Completed));
    }

    [Fact]
    public void ShouldKeepDuplicatesWhenRemoveDuplicatesIsFalse()
    {
        string first = TestMediaFactory.CreateWhiteJpg(_rootPath, "IMG_4001.JPG");
        string duplicate = TestMediaFactory.CreateWhiteJpg(_rootPath, "duplicate.jpg");
        TestMediaFactory.SetCreatedAt(first, new DateTimeOffset(2021, 5, 1, 8, 0, 0, TimeSpan.Zero));
        TestMediaFactory.SetCreatedAt(duplicate, new DateTimeOffset(2021, 5, 1, 9, 0, 0, TimeSpan.Zero));

        OrganizationResult organizationResult =
            MediaOrganizerService.Organize(DirectoryAnalyzer.Analyze(_rootPath), new OrganizationOptions(false, false));

        Assert.Equal(2, organizationResult.OrganizedMedia);
        Assert.Equal(0, organizationResult.RemovedDuplicates);
        Assert.True(File.Exists(Path.Combine(_rootPath, "1.jpg")));
        Assert.True(File.Exists(Path.Combine(_rootPath, "2.jpg")));
    }

    [Fact]
    public void ShouldThrowWhenAnalysisIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MediaOrganizerService.Organize(null!, new OrganizationOptions(false, false)));
    }

    [Fact]
    public void ShouldThrowWhenOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MediaOrganizerService.Organize(new DirectoryAnalysis(_rootPath, [], [], []), null!));
    }

    [Fact]
    public void ShouldThrowWhenFinalSequentialNameAlreadyExistsOutsideAnalysis()
    {
        string photo = TestMediaFactory.CreateWhiteJpg(_rootPath, "photo.jpg");
        string existingFinalName = TestMediaFactory.CreateBlueJpg(_rootPath, "1.jpg");
        MediaFile mediaFile = new(photo, ".jpg", new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), "hash-one");

        IOException e = Assert.Throws<IOException>(() => MediaOrganizerService.Organize(
            new DirectoryAnalysis(_rootPath, [mediaFile], [], []), new OrganizationOptions(false, false)));

        Assert.Contains(existingFinalName, e.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingProgress(List<OrganizationProgress> reports) : IProgress<OrganizationProgress>
    {
        public void Report(OrganizationProgress value) => reports.Add(value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, true);
    }
}
