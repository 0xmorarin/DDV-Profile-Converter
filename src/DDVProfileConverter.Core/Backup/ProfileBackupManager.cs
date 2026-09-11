using System.Security.Cryptography;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Backup;

public static class ProfileBackupManager
{
    private const string BackupDirectoryName = "backups";

    public static ProfileBackupResult Create(
        string sourceProfilePath,
        ReadOnlySpan<byte> originalEncryptedBytes,
        ProfileMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(sourceProfilePath))
        {
            throw new ArgumentException(
                "The source profile path is required.",
                nameof(sourceProfilePath));
        }

        if (originalEncryptedBytes.IsEmpty)
        {
            throw new InvalidDataException(
                "The original encrypted profile is empty.");
        }

        ArgumentNullException.ThrowIfNull(metadata);

        var canonicalFileName =
            ProfileBackupFileName.Build(metadata);

        var sourceFullPath =
            Path.GetFullPath(sourceProfilePath);

        var sourceDirectory =
            Path.GetDirectoryName(sourceFullPath);

        if (string.IsNullOrEmpty(sourceDirectory))
        {
            throw new InvalidDataException(
                "The source profile directory could not be determined.");
        }

        var backupDirectory = Path.Combine(
            sourceDirectory,
            BackupDirectoryName);

        Directory.CreateDirectory(backupDirectory);

        var sourceHash =
            SHA256.HashData(originalEncryptedBytes);

        for (var suffix = 1; ; suffix++)
        {
            var fileName = suffix == 1
                ? canonicalFileName
                : ProfileBackupFileName.AddCollisionSuffix(
                    canonicalFileName,
                    suffix);

            var destinationPath = Path.Combine(
                backupDirectory,
                fileName);

            if (File.Exists(destinationPath))
            {
                if (FileMatches(
                        destinationPath,
                        originalEncryptedBytes.Length,
                        sourceHash))
                {
                    return new ProfileBackupResult(
                        destinationPath,
                        false);
                }

                continue;
            }

            var tempPath = Path.Combine(
                backupDirectory,
                $".{Guid.NewGuid():N}.tmp");

            try
            {
                WriteAndVerifyTemp(
                    tempPath,
                    originalEncryptedBytes,
                    sourceHash);

                try
                {
                    File.Move(
                        tempPath,
                        destinationPath);

                    return new ProfileBackupResult(
                        destinationPath,
                        true);
                }
                catch (IOException) when (
                    File.Exists(destinationPath))
                {
                    if (FileMatches(
                            destinationPath,
                            originalEncryptedBytes.Length,
                            sourceHash))
                    {
                        return new ProfileBackupResult(
                            destinationPath,
                            false);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }

    private static void WriteAndVerifyTemp(
        string tempPath,
        ReadOnlySpan<byte> bytes,
        ReadOnlySpan<byte> expectedHash)
    {
        using (var stream = new FileStream(
                   tempPath,
                   FileMode.CreateNew,
                   FileAccess.Write,
                   FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        if (!FileMatches(
                tempPath,
                bytes.Length,
                expectedHash))
        {
            throw new IOException(
                "The backup file failed verification.");
        }
    }

    private static bool FileMatches(
        string path,
        int expectedLength,
        ReadOnlySpan<byte> expectedHash)
    {
        var info = new FileInfo(path);

        if (info.Length != expectedLength)
        {
            return false;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        var actualHash = SHA256.HashData(stream);

        return actualHash
            .AsSpan()
            .SequenceEqual(expectedHash);
    }
}
