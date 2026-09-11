using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public sealed record ProfileEncryptResult(
    string ProfilePath,
    ProfileMetadata Metadata);
