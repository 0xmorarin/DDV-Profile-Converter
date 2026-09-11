using System.Security.Cryptography;
using System.Text.Json;
using DDVProfileConverter.Core.Archive;
using DDVProfileConverter.Core.Crypto;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Conversion;

public static class ProfileReader
{
    public static ProfileReadResult Read(ReadOnlySpan<byte> input)
    {
        if (ProfileJson.TryReadMetadata(input, out var plainMetadata) &&
            plainMetadata is not null)
        {
            return new ProfileReadResult(
                ProfileFormat.PlainJson,
                plainMetadata,
                input.ToArray());
        }

        try
        {
            var archiveBytes = ProfileCrypto.Decrypt(input);
            var jsonBytes = ProfileArchive.ExtractProfile(archiveBytes);
            var metadata = ProfileJson.ReadMetadata(jsonBytes);

            return new ProfileReadResult(
                ProfileFormat.Encrypted,
                metadata,
                jsonBytes);
        }
        catch (Exception exception) when (
            exception is CryptographicException or
            InvalidDataException or
            JsonException)
        {
            throw new InvalidDataException(
                "The file is neither a valid DDV plaintext profile nor a valid encrypted DDV profile.",
                exception);
        }
    }
}
