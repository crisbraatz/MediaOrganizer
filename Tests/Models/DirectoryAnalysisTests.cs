using MediaOrganizer.Models;

namespace Tests.Models;

public class DirectoryAnalysisTests
{
    [Fact]
    public void ShouldDeriveTotalsFromMediaDuplicatesAndSubdirectories()
    {
        MediaFile photo = new("photo.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile video = new("video.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-two");
        MediaFile duplicatePhoto = new("photo-copy.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        DuplicateGroup duplicateGroup = new("hash-one", photo, [duplicatePhoto]);

        DirectoryAnalysis directoryAnalysis =
            new("root", [photo, video, duplicatePhoto], [duplicateGroup], ["root/sub"]);

        Assert.Equal(3, directoryAnalysis.TotalMedia);
        Assert.Equal(2, directoryAnalysis.TotalPhotos);
        Assert.Equal(1, directoryAnalysis.TotalVideos);
        Assert.Equal(1, directoryAnalysis.TotalDuplicateMedia);
        Assert.Equal(1, directoryAnalysis.TotalDuplicatePhotos);
        Assert.Equal(0, directoryAnalysis.TotalDuplicateVideos);
        Assert.Equal(1, directoryAnalysis.TotalSubdirectories);
    }

    [Fact]
    public void ShouldGroupExtensionCountsCaseInsensitively()
    {
        MediaFile first = new("one.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile second = new("two.JPG", ".JPG", DateTimeOffset.UnixEpoch, "hash-two");
        DirectoryAnalysis directoryAnalysis = new("root", [first, second], [], []);

        ExtensionCount extensionCount = Assert.Single(directoryAnalysis.GetExtensionCounts());

        Assert.Equal(".jpg", extensionCount.Extension);
        Assert.Equal(2, extensionCount.Count);
    }

    [Fact]
    public void ShouldOrderExtensionCountsByExtension()
    {
        MediaFile video = new("video.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile photo = new("photo.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-two");
        MediaFile image = new("image.heic", ".heic", DateTimeOffset.UnixEpoch, "hash-three");
        DirectoryAnalysis directoryAnalysis = new("root", [video, photo, image], [], []);

        IList<ExtensionCount> extensionCounts = directoryAnalysis.GetExtensionCounts();

        Assert.Equal([".heic", ".jpg", ".mp4"], extensionCounts.Select(static x => x.Extension));
    }

    [Fact]
    public void ShouldOrderDuplicateExtensionCountsByExtension()
    {
        MediaFile firstKeeper = new("photo.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile firstDuplicate = new("copy.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile secondKeeper = new("video.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-two");
        MediaFile secondDuplicate = new("copy.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-two");
        DirectoryAnalysis directoryAnalysis = new(
            "root",
            [firstKeeper, firstDuplicate, secondKeeper, secondDuplicate],
            [
                new DuplicateGroup("hash-two", secondKeeper, [secondDuplicate]),
                new DuplicateGroup("hash-one", firstKeeper, [firstDuplicate])
            ],
            []);

        IList<ExtensionCount> extensionCounts = directoryAnalysis.GetDuplicateExtensionCounts();

        Assert.Equal([".jpg", ".mp4"], extensionCounts.Select(static x => x.Extension));
    }

    [Fact]
    public void ShouldSumAllDuplicateFiles()
    {
        MediaFile firstKeeper = new("one.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile firstDuplicate = new("one-copy.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile secondDuplicate = new("one-copy-2.jpg", ".jpg", DateTimeOffset.UnixEpoch, "hash-one");
        MediaFile secondKeeper = new("two.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-two");
        MediaFile thirdDuplicate = new("two-copy.mp4", ".mp4", DateTimeOffset.UnixEpoch, "hash-two");

        DirectoryAnalysis directoryAnalysis = new(
            "root",
            [firstKeeper, firstDuplicate, secondDuplicate, secondKeeper, thirdDuplicate],
            [
                new DuplicateGroup("hash-one", firstKeeper, [firstDuplicate, secondDuplicate]),
                new DuplicateGroup("hash-two", secondKeeper, [thirdDuplicate])
            ],
            []);

        Assert.Equal(3, directoryAnalysis.TotalDuplicateMedia);
    }
}
