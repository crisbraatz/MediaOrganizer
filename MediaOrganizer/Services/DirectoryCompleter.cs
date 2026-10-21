using MediaOrganizer.Models;

namespace MediaOrganizer.Services;

internal static class DirectoryCompleter
{
    internal static DirectoryCompletion Complete(string input, string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        string parentExpression = string.Empty;

        if (input.Length > 0)
            if (EndsWithDirectorySeparator(input))
                parentExpression = input;
            else
                parentExpression = Path.GetDirectoryName(input) ?? string.Empty;

        string parentPath = currentDirectory;

        if (!string.IsNullOrEmpty(parentExpression))
            parentPath = Path.IsPathRooted(parentExpression)
                ? Path.GetFullPath(parentExpression)
                : Path.GetFullPath(Path.Combine(currentDirectory, parentExpression));

        if (!Directory.Exists(parentPath))
            return new DirectoryCompletion(input, []);

        string partialName = EndsWithDirectorySeparator(input) ? string.Empty : Path.GetFileName(input);
        List<string> suggestions =
        [
            .. Directory.GetDirectories(parentPath)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(x => x.StartsWith(partialName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];

        if (suggestions.Count is 0)
            return new DirectoryCompletion(input, []);

        string prefix = suggestions[0];

        foreach (string suggestion in suggestions.Skip(1))
        {
            int length = 0;

            while (length < prefix.Length &&
                   length < suggestion.Length &&
                   char.ToUpperInvariant(prefix[length]) == char.ToUpperInvariant(suggestion[length]))
                length++;

            prefix = prefix[..length];
        }

        string completedName = prefix.Length > partialName.Length || suggestions.Count is 1 ? prefix : partialName;
        string value = completedName;

        if (!string.IsNullOrEmpty(parentExpression))
            value = EndsWithDirectorySeparator(parentExpression)
                ? string.Concat(parentExpression, completedName)
                : string.Concat(parentExpression, Path.DirectorySeparatorChar, completedName);

        if (suggestions.Count is 1)
            value += Path.DirectorySeparatorChar;

        return new DirectoryCompletion(value, suggestions);
    }

    private static bool EndsWithDirectorySeparator(string value) =>
        value.EndsWith(Path.DirectorySeparatorChar) || value.EndsWith(Path.AltDirectorySeparatorChar);
}
