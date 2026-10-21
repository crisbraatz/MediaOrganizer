namespace MediaOrganizer.Models;

internal sealed record OrganizationResult(
    int OrganizedMedia,
    int ProcessedDirectories,
    int RemovedDuplicates,
    int RemovedSubdirectories);
