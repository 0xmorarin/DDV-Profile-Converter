namespace DDVProfileConverter.Core.Profile;

public sealed record ProfileMetadata(
    int Version,
    string? LastCustomIdOwner,
    string? Created,
    string? Modified,
    string? PlayerName,
    long? TimePlayedInMinutes,
    string? LastSaveDeviceType);
