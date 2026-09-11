using DDVProfileConverter.Core.Backup;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public sealed record ProfileDecryptResult(
    string ProfilePath,
    ProfileMetadata Metadata,
    ProfileBackupResult Backup);
