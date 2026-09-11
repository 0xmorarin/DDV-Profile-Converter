namespace DDVProfileConverter.Core.Backup;

public sealed record ProfileBackupResult(
    string Path,
    bool Created);
