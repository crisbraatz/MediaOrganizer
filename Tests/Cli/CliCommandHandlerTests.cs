using MediaOrganizer.Cli;

namespace Tests.Cli;

public class CliCommandHandlerTests
{
    [Theory]
    [InlineData("exit")]
    [InlineData(" EXIT ")]
    [InlineData("Exit")]
    public void ShouldReturnExitForExitCommand(string value)
    {
        Assert.Equal(CliCommand.Exit, CliCommandHandler.Parse(value));
    }

    [Theory]
    [InlineData("restart")]
    [InlineData(" RESTART ")]
    [InlineData("Restart")]
    public void ShouldReturnRestartForRestartCommand(string value)
    {
        Assert.Equal(CliCommand.Restart, CliCommandHandler.Parse(value));
    }

    [Fact]
    public void ShouldThrowRestartCancellationForRestartCommand()
    {
        OperationCanceledException e =
            Assert.Throws<OperationCanceledException>(() => CliCommandHandler.Handle("restart"));

        Assert.Equal(CliCommandHandler.RestartMessage, e.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("yes")]
    [InlineData("restart now")]
    public void ShouldReturnNoneForUnknownCommand(string value)
    {
        Assert.Equal(CliCommand.None, CliCommandHandler.Parse(value));
    }
}
