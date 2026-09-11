using DDVProfileConverter.Core.Backup;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public sealed record ProfileConversionResult(
    string ProfilePath,
    ProfileFormat InputFormat,
    ProfileFormat OutputFormat,
    ProfileMetadata Metadata,
    ProfileBackupResult? Backup);
