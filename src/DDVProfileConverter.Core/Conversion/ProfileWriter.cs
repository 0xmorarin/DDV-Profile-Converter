using DDVProfileConverter.Core.Archive;
using DDVProfileConverter.Core.Crypto;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public static class ProfileWriter
{
    public static ProfileWriteResult Encrypt(
        ReadOnlySpan<byte> json)
    {
        var compactJson = ProfileJson.Minify(json);
        var metadata = ProfileJson.ReadMetadata(compactJson);

        var archiveBytes =
            ProfileArchive.CreateProfileArchive(compactJson);

        var encryptedBytes =
            ProfileCrypto.Encrypt(archiveBytes);

        ValidateRoundTrip(
            encryptedBytes,
            compactJson);

        return new ProfileWriteResult(
            metadata,
            encryptedBytes);
    }

    private static void ValidateRoundTrip(
        ReadOnlySpan<byte> encryptedBytes,
        ReadOnlySpan<byte> expectedJson)
    {
        var archiveBytes =
            ProfileCrypto.Decrypt(encryptedBytes);

        var actualJson =
            ProfileArchive.ExtractProfile(archiveBytes);

        if (!actualJson.AsSpan().SequenceEqual(expectedJson))
        {
            throw new InvalidDataException(
                "Encrypted profile round-trip validation failed.");
        }

        _ = ProfileJson.ReadMetadata(actualJson);
    }
}
