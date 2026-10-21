namespace MediaOrganizer.Cli;

internal static class CliCommandHandler
{
    internal const string RestartMessage = "Restart requested.";

    internal static void Handle(string value)
    {
        switch (Parse(value))
        {
            case CliCommand.Restart:
                throw new OperationCanceledException(RestartMessage);
            case CliCommand.Exit:
                Environment.Exit(0);

                break;
            case CliCommand.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    internal static CliCommand Parse(string value)
    {
        string command = value.Trim();

        if (string.Equals(command, "restart", StringComparison.OrdinalIgnoreCase))
            return CliCommand.Restart;

        if (string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase))
            return CliCommand.Exit;

        return CliCommand.None;
    }
}
