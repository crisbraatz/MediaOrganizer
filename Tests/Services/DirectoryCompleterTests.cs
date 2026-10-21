using MediaOrganizer.Models;
using MediaOrganizer.Services;

namespace Tests.Services;

public sealed class DirectoryCompleterTests : IDisposable
{
    private readonly string _rootPath =
        Path.Combine(Path.GetTempPath(), $"mediaorganizer-completion-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ShouldReturnSingleSuggestionDirectoryWithTrailingSeparator()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Pictures"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Videos"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Pic", _rootPath);

        Assert.Equal($"Pictures{Path.DirectorySeparatorChar}", directoryCompletion.Value);
        Assert.Single(directoryCompletion.Suggestions);
        Assert.Equal("Pictures", directoryCompletion.Suggestions[0]);
    }

    [Fact]
    public void ShouldReturnCommonPrefixAndSuggestionsForMultipleDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Photos 2024"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Photos 2025"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Pho", _rootPath);

        Assert.Equal("Photos 202", directoryCompletion.Value);
        Assert.Equal(2, directoryCompletion.Suggestions.Count);
        Assert.Equal(["Photos 2024", "Photos 2025"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldReturnShortestCommonPrefixAcrossAllSuggestions()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Photos Alpha"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Photos Beta"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Photos"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Pho", _rootPath);

        Assert.Equal("Photos", directoryCompletion.Value);
        Assert.Equal(["Photos", "Photos Alpha", "Photos Beta"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldUseRelativeParentPath()
    {
        Directory.CreateDirectory(Path.Combine(Path.Combine(_rootPath, "Media"), "Trip"));

        DirectoryCompletion directoryCompletion =
            DirectoryCompleter.Complete($"Media{Path.DirectorySeparatorChar}Tr", _rootPath);

        Assert.Equal($"Media{Path.DirectorySeparatorChar}Trip{Path.DirectorySeparatorChar}", directoryCompletion.Value);
        Assert.Single(directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldUseAbsoluteParentPath()
    {
        string mediaRoot = Path.Combine(_rootPath, "Media");
        Directory.CreateDirectory(Path.Combine(mediaRoot, "Trip"));

        DirectoryCompletion directoryCompletion =
            DirectoryCompleter.Complete(Path.Combine(mediaRoot, "Tr"), _rootPath);

        Assert.Equal(Path.Combine(mediaRoot, $"Trip{Path.DirectorySeparatorChar}"), directoryCompletion.Value);
        Assert.Equal(["Trip"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldUseAbsoluteParentPathWithDirectorySeparator()
    {
        string mediaRoot = Path.Combine(_rootPath, "Media");
        Directory.CreateDirectory(Path.Combine(mediaRoot, "B-Roll"));
        Directory.CreateDirectory(Path.Combine(mediaRoot, "Camera A"));

        DirectoryCompletion directoryCompletion =
            DirectoryCompleter.Complete(mediaRoot + Path.DirectorySeparatorChar, _rootPath);

        Assert.Equal(mediaRoot + Path.DirectorySeparatorChar, directoryCompletion.Value);
        Assert.Equal(["B-Roll", "Camera A"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldListChildrenWhenInputEndsWithDirectorySeparator()
    {
        string mediaRoot = Path.Combine(_rootPath, "Media");
        Directory.CreateDirectory(Path.Combine(mediaRoot, "B-Roll"));
        Directory.CreateDirectory(Path.Combine(mediaRoot, "Camera A"));

        DirectoryCompletion directoryCompletion =
            DirectoryCompleter.Complete($"Media{Path.DirectorySeparatorChar}", _rootPath);

        Assert.Equal($"Media{Path.DirectorySeparatorChar}", directoryCompletion.Value);
        Assert.Equal(["B-Roll", "Camera A"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldKeepFullInputAndAddSeparatorWhenItAlreadySuggestedOnlyDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Camera"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Camera", _rootPath);

        Assert.Equal($"Camera{Path.DirectorySeparatorChar}", directoryCompletion.Value);
        Assert.Equal(["Camera"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldKeepInputWhenMultipleSuggestionsHaveNoLongerCommonPrefix()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Camera"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Cloud"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("C", _rootPath);

        Assert.Equal("C", directoryCompletion.Value);
        Assert.Equal(["Camera", "Cloud"], directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldReturnInputWhenParentDoesNotExist()
    {
        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Missing/Tr", _rootPath);

        Assert.Equal("Missing/Tr", directoryCompletion.Value);
        Assert.Empty(directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldReturnInputWhenNoDirectoryIsSuggested()
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, "Pictures"));

        DirectoryCompletion directoryCompletion = DirectoryCompleter.Complete("Vid", _rootPath);

        Assert.Equal("Vid", directoryCompletion.Value);
        Assert.Empty(directoryCompletion.Suggestions);
    }

    [Fact]
    public void ShouldThrowForNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => DirectoryCompleter.Complete(null!, _rootPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ShouldThrowForMissingCurrentDirectory(string currentDirectory)
    {
        Assert.Throws<ArgumentException>(() => DirectoryCompleter.Complete("Pic", currentDirectory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, true);
    }
}
