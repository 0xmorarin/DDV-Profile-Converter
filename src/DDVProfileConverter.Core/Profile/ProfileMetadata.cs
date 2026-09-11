namespace DDVProfileConverter.Core.Profile;

public sealed record ProfileMetadata(
    int Version,
    string? LastCustomIdOwner,
    string? Modified,
    string? PlayerName);
