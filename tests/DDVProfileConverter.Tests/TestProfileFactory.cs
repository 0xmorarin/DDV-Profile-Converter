using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace DDVProfileConverter.Tests;

internal static class TestProfileFactory
{
    private const string HexKey = "62357168683873614A38556C444A557A545A5864325467366D626F3857386e35";

    public const string JsonText = "{\"GameInfo\":{\"Version\":608,\"LastCustomIdOwner\":\"0123456789ABCDEF\",\"Created\":\"2025-10-14T23:31:58.070966900Z\",\"Modified\":\"2026-09-11T00:00:00.123456700Z\",\"LastSaveDeviceInfo\":{\"deviceType\":\"DeviceType_Windows\"}},\"Player\":{\"Name\":\"TestPlayer\",\"TimePlayedInMinutes\":18867},\"Unknown\":{\"Value\":123}}";

    public static byte[] CreateJson()
    {
        return Encoding.UTF8.GetBytes(JsonText);
    }

    public static byte[] CreateArchive(params (string Name, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(
                   stream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            foreach (var entryData in entries)
            {
                var entry = archive.CreateEntry(
                    entryData.Name,
                    CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                entryStream.Write(
                    entryData.Content,
                    0,
                    entryData.Content.Length);
            }
        }

        return stream.ToArray();
    }

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        using var aes = Aes.Create();

        aes.KeySize = 256;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Convert.FromHexString(HexKey);

        using var encryptor = aes.CreateEncryptor();

        var input = plaintext.ToArray();

        return encryptor.TransformFinalBlock(
            input,
            0,
            input.Length);
    }

    public static byte[] CreateEncryptedProfile()
    {
        var json = CreateJson();
        var archive = CreateArchive(("profile", json));

        return Encrypt(archive);
    }
}
