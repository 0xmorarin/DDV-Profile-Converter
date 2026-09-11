using DDVProfileConverter.Core.Backup;
using DDVProfileConverter.Core.IO;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public static class ProfileFileDecryptor
{
    public static ProfileDecryptResult DecryptInPlace(
        string profilePath)
    {
        if (string.IsNullOrWhiteSpace(profilePath))
        {
            throw new ArgumentException(
                "The profile path is required.",
                nameof(profilePath));
        }

        var fullPath =
            Path.GetFullPath(profilePath);

        var originalBytes =
            File.ReadAllBytes(fullPath);

        var readResult =
            ProfileReader.Read(originalBytes);

        if (readResult.Format != ProfileFormat.Encrypted)
        {
            throw new InvalidDataException(
                "The profile is already plaintext.");
        }

        var prettyJson =
            ProfileJson.PrettyPrint(readResult.JsonBytes);

        var prettyMetadata =
            ProfileJson.ReadMetadata(prettyJson);

        if (prettyMetadata != readResult.Metadata)
        {
            throw new InvalidDataException(
                "Plaintext profile metadata changed during formatting.");
        }

        var backup =
            ProfileBackupManager.Create(
                fullPath,
                originalBytes,
                readResult.Metadata);

        VerifiedFileReplacer.Replace(
            fullPath,
            prettyJson,
            originalBytes,
            ValidatePlaintext);

        return new ProfileDecryptResult(
            fullPath,
            readResult.Metadata,
            backup);
    }

    private static void ValidatePlaintext(byte[] bytes)
    {
        var result =
            ProfileReader.Read(bytes);

        if (result.Format != ProfileFormat.PlainJson)
        {
            throw new InvalidDataException(
                "The temporary plaintext profile failed validation.");
        }
    }
}
