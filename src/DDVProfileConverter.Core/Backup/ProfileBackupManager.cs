using System.Security.Cryptography;
using System.Text.Json;
using DDVProfileConverter.Core.Archive;
using DDVProfileConverter.Core.Crypto;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Backup;

public static class ProfileBackupManager
{
    public static ProfileBackupResult Create(
        string backupDirectoryPath,
        ReadOnlySpan<byte> originalEncryptedBytes,
        ReadOnlySpan<byte> profileJsonBytes,
        ProfileMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(backupDirectoryPath))
        {
            throw new ArgumentException(
                "The backup directory path is required.",
                nameof(backupDirectoryPath));
        }

        if (originalEncryptedBytes.IsEmpty)
        {
            throw new InvalidDataException(
                "The original encrypted profile is empty.");
        }

        if (profileJsonBytes.IsEmpty)
        {
            throw new InvalidDataException(
                "The decrypted profile JSON is empty.");
        }

        ArgumentNullException.ThrowIfNull(metadata);

        var canonicalFileName =
            ProfileBackupFileName.Build(metadata);

        var backupDirectory =
            Path.GetFullPath(backupDirectoryPath);

        Directory.CreateDirectory(backupDirectory);

        var sourceHash =
            SHA256.HashData(originalEncryptedBytes);

        var normalizedJsonHash =
            SHA256.HashData(
                ProfileJson.Minify(profileJsonBytes));

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
                        sourceHash) ||
                    ProfileContentMatches(
                        destinationPath,
                        normalizedJsonHash))
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
                            sourceHash) ||
                        ProfileContentMatches(
                            destinationPath,
                            normalizedJsonHash))
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

    private static bool ProfileContentMatches(
        string path,
        ReadOnlySpan<byte> expectedHash)
    {
        try
        {
            var encryptedBytes =
                File.ReadAllBytes(path);

            var archiveBytes =
                ProfileCrypto.Decrypt(encryptedBytes);

            var jsonBytes =
                ProfileArchive.ExtractProfile(archiveBytes);

            var normalizedJson =
                ProfileJson.Minify(jsonBytes);

            var actualHash =
                SHA256.HashData(normalizedJson);

            return actualHash
                .AsSpan()
                .SequenceEqual(expectedHash);
        }
        catch (Exception exception) when (
            exception is CryptographicException or
            InvalidDataException or
            JsonException or
            IOException or
            UnauthorizedAccessException)
        {
            return false;
        }
    }
}
