namespace OpenGameVerse.Core.Models;

public sealed record GameStatusTarget(
    long Id,
    string? Title,
    string? ExecutablePath,
    string? InstallPath,
    string? PlatformId
);
