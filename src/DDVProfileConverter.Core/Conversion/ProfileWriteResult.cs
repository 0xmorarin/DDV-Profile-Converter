using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public sealed record ProfileWriteResult(
    ProfileMetadata Metadata,
    byte[] EncryptedBytes);
