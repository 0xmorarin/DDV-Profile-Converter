using System.Globalization;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Core.Backup;

public static class ProfileBackupFileName
{
    private const string Extension = ".profile.bak";

    public static string Build(ProfileMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(metadata.LastCustomIdOwner))
        {
            throw new InvalidDataException(
                "GameInfo.LastCustomIdOwner is required for the backup filename.");
        }

        if (!IsSafeIdentity(metadata.LastCustomIdOwner))
        {
            throw new InvalidDataException(
                "GameInfo.LastCustomIdOwner contains invalid filename characters.");
        }

        var timestamp = NormalizeModified(metadata.Modified);

        return $"mdc{metadata.LastCustomIdOwner}_v{metadata.Version}_{timestamp}{Extension}";
    }

    public static string AddCollisionSuffix(
        string canonicalFileName,
        int suffix)
    {
        if (suffix < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(suffix));
        }

        if (!canonicalFileName.EndsWith(
                Extension,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The backup filename is invalid.",
                nameof(canonicalFileName));
        }

        var stem = canonicalFileName[..^Extension.Length];

        return $"{stem}_{suffix.ToString(CultureInfo.InvariantCulture)}{Extension}";
    }

    private static string NormalizeModified(string? modified)
    {
        if (string.IsNullOrWhiteSpace(modified))
        {
            throw new InvalidDataException(
                "GameInfo.Modified is required for the backup filename.");
        }

        if (modified.Length < 20 ||
            modified[4] != '-' ||
            modified[7] != '-' ||
            modified[10] != 'T' ||
            modified[13] != ':' ||
            modified[16] != ':' ||
            modified[^1] != 'Z')
        {
            throw new InvalidDataException(
                "GameInfo.Modified is not a supported UTC timestamp.");
        }

        var fractionalLength = modified.Length - 20;

        if (fractionalLength > 0)
        {
            if (fractionalLength < 2 ||
                fractionalLength > 10 ||
                modified[19] != '.')
            {
                throw new InvalidDataException(
                    "GameInfo.Modified is not a supported UTC timestamp.");
            }

            for (var index = 20; index < modified.Length - 1; index++)
            {
                if (!char.IsAsciiDigit(modified[index]))
                {
                    throw new InvalidDataException(
                        "GameInfo.Modified is not a supported UTC timestamp.");
                }
            }
        }

        if (!DateTime.TryParseExact(
                modified[..19],
                "yyyy-MM-dd'T'HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var value))
        {
            throw new InvalidDataException(
                "GameInfo.Modified is not a supported UTC timestamp.");
        }

        return value.ToString(
            "yyyyMMdd'T'HHmmss'Z'",
            CultureInfo.InvariantCulture);
    }

    private static bool IsSafeIdentity(string value)
    {
        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) &&
                character != '-' &&
                character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
