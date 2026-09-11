using System.IO.Compression;

namespace DDVProfileConverter.Core.Archive;

public static class ProfileArchive
{
    private const string ProfileEntryName = "profile";

    public static byte[] CreateProfileArchive(ReadOnlySpan<byte> profileBytes)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(
                   stream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            var entry = archive.CreateEntry(
                ProfileEntryName,
                CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            entryStream.Write(profileBytes);
        }

        return stream.ToArray();
    }

    public static byte[] ExtractProfile(ReadOnlySpan<byte> archiveBytes)
    {
        using var stream = new MemoryStream(
            archiveBytes.ToArray(),
            writable: false);

        using var archive = new ZipArchive(
            stream,
            ZipArchiveMode.Read,
            leaveOpen: false);

        var entries = archive.Entries
            .Where(entry => string.Equals(
                entry.FullName,
                ProfileEntryName,
                StringComparison.Ordinal))
            .ToArray();

        if (entries.Length != 1)
        {
            throw new InvalidDataException(
                "The archive must contain exactly one profile entry.");
        }

        using var entryStream = entries[0].Open();
        using var output = new MemoryStream();

        entryStream.CopyTo(output);

        return output.ToArray();
    }
}
