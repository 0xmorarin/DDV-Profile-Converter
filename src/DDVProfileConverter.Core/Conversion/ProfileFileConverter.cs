namespace DDVProfileConverter.Core.Conversion;

public static class ProfileFileConverter
{
    public static ProfileConversionResult ConvertInPlace(
        string profilePath,
        string backupDirectory)
    {
        if (string.IsNullOrWhiteSpace(profilePath))
        {
            throw new ArgumentException(
                "The profile path is required.",
                nameof(profilePath));
        }

        var fullPath =
            Path.GetFullPath(profilePath);

        var inputBytes =
            File.ReadAllBytes(fullPath);

        var readResult =
            ProfileReader.Read(inputBytes);

        return readResult.Format switch
        {
            ProfileFormat.Encrypted =>
                Decrypt(
                    fullPath,
                    backupDirectory),

            ProfileFormat.PlainJson =>
                Encrypt(fullPath),

            _ => throw new InvalidDataException(
                "The profile format is not supported.")
        };
    }

    private static ProfileConversionResult Decrypt(
        string profilePath,
        string backupDirectory)
    {
        var result =
            ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory);

        return new ProfileConversionResult(
            result.ProfilePath,
            ProfileFormat.Encrypted,
            ProfileFormat.PlainJson,
            result.Metadata,
            result.Backup);
    }

    private static ProfileConversionResult Encrypt(
        string profilePath)
    {
        var result =
            ProfileFileEncryptor.EncryptInPlace(
                profilePath);

        return new ProfileConversionResult(
            result.ProfilePath,
            ProfileFormat.PlainJson,
            ProfileFormat.Encrypted,
            result.Metadata,
            null);
    }
}
