using MediaOrganizer.Services;

namespace Tests.Services;

public class MediaExtensionHelperTests
{
    [Theory]
    [InlineData(".arw")]
    [InlineData(".bmp")]
    [InlineData(".cr2")]
    [InlineData(".cr3")]
    [InlineData(".dng")]
    [InlineData(".gif")]
    [InlineData(".jpg")]
    [InlineData(".JPEG")]
    [InlineData(".heic")]
    [InlineData(".heif")]
    [InlineData(".nef")]
    [InlineData(".orf")]
    [InlineData(".png")]
    [InlineData(".raf")]
    [InlineData(".rw2")]
    [InlineData(".tif")]
    [InlineData(".tiff")]
    [InlineData(".webp")]
    public void ShouldReturnTrueForSupportedPhotoExtensions(string extension)
    {
        Assert.True(MediaExtensionHelper.IsPhotoExtension(extension));
        Assert.False(MediaExtensionHelper.IsVideoExtension(extension));
    }

    [Theory]
    [InlineData(".3gp")]
    [InlineData(".avi")]
    [InlineData(".m4v")]
    [InlineData(".mkv")]
    [InlineData(".mov")]
    [InlineData(".mp4")]
    [InlineData(".mpeg")]
    [InlineData(".mpg")]
    [InlineData(".mts")]
    [InlineData(".webm")]
    [InlineData(".wmv")]
    public void ShouldReturnTrueForSupportedVideoExtensions(string extension)
    {
        Assert.True(MediaExtensionHelper.IsVideoExtension(extension));
        Assert.False(MediaExtensionHelper.IsPhotoExtension(extension));
    }

    [Fact]
    public void ShouldNormalizeSupportedExtension()
    {
        bool result = MediaExtensionHelper.TryGetExtension("IMG_0001.JPG", out string extension);

        Assert.True(result);
        Assert.Equal(".jpg", extension);
    }

    [Theory]
    [InlineData("photo.ARW", ".arw")]
    [InlineData("photo.BMP", ".bmp")]
    [InlineData("photo.CR2", ".cr2")]
    [InlineData("photo.CR3", ".cr3")]
    [InlineData("photo.DNG", ".dng")]
    [InlineData("photo.GIF", ".gif")]
    [InlineData("photo.HEIC", ".heic")]
    [InlineData("photo.HEIF", ".heif")]
    [InlineData("photo.JPEG", ".jpeg")]
    [InlineData("photo.JPG", ".jpg")]
    [InlineData("photo.NEF", ".nef")]
    [InlineData("photo.ORF", ".orf")]
    [InlineData("photo.PNG", ".png")]
    [InlineData("photo.RAF", ".raf")]
    [InlineData("photo.RW2", ".rw2")]
    [InlineData("photo.TIF", ".tif")]
    [InlineData("photo.TIFF", ".tiff")]
    [InlineData("photo.WEBP", ".webp")]
    [InlineData("video.3GP", ".3gp")]
    [InlineData("video.AVI", ".avi")]
    [InlineData("video.M4V", ".m4v")]
    [InlineData("video.MKV", ".mkv")]
    [InlineData("video.MOV", ".mov")]
    [InlineData("video.MP4", ".mp4")]
    [InlineData("video.MPEG", ".mpeg")]
    [InlineData("video.MPG", ".mpg")]
    [InlineData("video.MTS", ".mts")]
    [InlineData("video.WEBM", ".webm")]
    [InlineData("video.WMV", ".wmv")]
    public void ShouldNormalizeEverySupportedExtension(string path, string expectedExtension)
    {
        bool result = MediaExtensionHelper.TryGetExtension(path, out string extension);

        Assert.True(result);
        Assert.Equal(expectedExtension, extension);
    }

    [Fact]
    public void ShouldReturnFalseForUnsupportedExtension()
    {
        bool result = MediaExtensionHelper.TryGetExtension("notes.txt", out string extension);

        Assert.False(result);
        Assert.Equal(string.Empty, extension);
    }
}
