namespace MediaOrganizer.Models;

internal sealed record MediaFile(string Path, string Extension, DateTimeOffset CapturedAt, string Hash);
