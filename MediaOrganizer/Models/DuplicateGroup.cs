namespace MediaOrganizer.Models;

internal sealed record DuplicateGroup(string Hash, MediaFile Keeper, IList<MediaFile> Duplicates);
