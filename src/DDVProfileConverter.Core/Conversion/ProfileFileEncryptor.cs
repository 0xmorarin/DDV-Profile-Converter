using DDVProfileConverter.Core.IO;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public static class ProfileFileEncryptor
{
    public static ProfileEncryptResult EncryptInPlace(
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

        if (readResult.Format != ProfileFormat.PlainJson)
        {
            throw new InvalidDataException(
                "The profile is already encrypted.");
        }

        var writeResult =
            ProfileWriter.Encrypt(originalBytes);

        if (writeResult.Metadata != readResult.Metadata)
        {
            throw new InvalidDataException(
                "Profile metadata changed during encryption.");
        }

        VerifiedFileReplacer.Replace(
            fullPath,
            writeResult.EncryptedBytes,
            originalBytes,
            bytes => ValidateEncrypted(
                bytes,
                readResult.Metadata));

        return new ProfileEncryptResult(
            fullPath,
            readResult.Metadata);
    }

    private static void ValidateEncrypted(
        byte[] bytes,
        ProfileMetadata expectedMetadata)
    {
        var result =
            ProfileReader.Read(bytes);

        if (result.Format != ProfileFormat.Encrypted)
        {
            throw new InvalidDataException(
                "The temporary encrypted profile failed validation.");
        }

        if (result.Metadata != expectedMetadata)
        {
            throw new InvalidDataException(
                "The temporary encrypted profile metadata is invalid.");
        }
    }
}
