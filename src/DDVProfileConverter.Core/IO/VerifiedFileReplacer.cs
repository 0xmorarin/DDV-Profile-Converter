namespace DDVProfileConverter.Core.IO;

internal static class VerifiedFileReplacer
{
    public static void Replace(
        string destinationPath,
        ReadOnlySpan<byte> outputBytes,
        ReadOnlySpan<byte> expectedOriginalBytes,
        Action<byte[]> validator)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException(
                "The destination path is required.",
                nameof(destinationPath));
        }

        ArgumentNullException.ThrowIfNull(validator);

        var destinationFullPath =
            Path.GetFullPath(destinationPath);

        if (!File.Exists(destinationFullPath))
        {
            throw new FileNotFoundException(
                "The destination file does not exist.",
                destinationFullPath);
        }

        var directory =
            Path.GetDirectoryName(destinationFullPath);

        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidDataException(
                "The destination directory could not be determined.");
        }

        var fileName =
            Path.GetFileName(destinationFullPath);

        var tempPath = Path.Combine(
            directory,
            $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                stream.Write(outputBytes);
                stream.Flush(flushToDisk: true);
            }

            var tempBytes =
                File.ReadAllBytes(tempPath);

            if (!tempBytes
                    .AsSpan()
                    .SequenceEqual(outputBytes))
            {
                throw new IOException(
                    "The temporary file failed byte verification.");
            }

            validator(tempBytes);

            var currentBytes =
                File.ReadAllBytes(destinationFullPath);

            if (!currentBytes
                    .AsSpan()
                    .SequenceEqual(expectedOriginalBytes))
            {
                throw new IOException(
                    "The source profile changed during conversion.");
            }

            File.Move(
                tempPath,
                destinationFullPath,
                overwrite: true);
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
