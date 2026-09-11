using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public sealed record ProfileReadResult(
    ProfileFormat Format,
    ProfileMetadata Metadata,
    byte[] JsonBytes);
