using System.Text;
using MediaOrganizer.Cli;

namespace Tests.Cli;

public class CliPromptsTests
{
    [Fact]
    public void ShouldAddPrintableCharactersToCommand()
    {
        StringBuilder command = new();

        CliPrompts.AppendCommandKey(command, new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false));
        CliPrompts.AppendCommandKey(command, new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false));

        Assert.Equal("re", command.ToString());
    }

    [Fact]
    public void ShouldRemoveLastCharacterOnBackspace()
    {
        StringBuilder command = new("res");

        CliPrompts.AppendCommandKey(command, new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("re", command.ToString());
    }

    [Fact]
    public void ShouldKeepEmptyCommandOnBackspace()
    {
        StringBuilder command = new();

        CliPrompts.AppendCommandKey(command, new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal(string.Empty, command.ToString());
    }

    [Fact]
    public void ShouldIgnoreControlCharacters()
    {
        StringBuilder command = new("re");

        CliPrompts.AppendCommandKey(command, new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));

        Assert.Equal("re", command.ToString());
    }

    [Fact]
    public void ShouldThrowForNullCommand()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CliPrompts.AppendCommandKey(null!, new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false)));
    }
}
