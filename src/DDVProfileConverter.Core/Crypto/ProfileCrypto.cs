using System.Security.Cryptography;

namespace DDVProfileConverter.Core.Crypto;

public static class ProfileCrypto
{
    private const string HexKey = "62357168683873614A38556C444A557A545A5864325467366D626F3857386e35";
    private static readonly byte[] Key = Convert.FromHexString(HexKey);

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        if (plaintext.IsEmpty)
        {
            throw new CryptographicException(
                "The profile payload is empty.");
        }

        using var aes = CreateAes();
        using var encryptor = aes.CreateEncryptor();

        var input = plaintext.ToArray();

        return encryptor.TransformFinalBlock(
            input,
            0,
            input.Length);
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> ciphertext)
    {
        if (ciphertext.IsEmpty ||
            ciphertext.Length % 16 != 0)
        {
            throw new CryptographicException(
                "The encrypted profile length is invalid.");
        }

        using var aes = CreateAes();
        using var decryptor = aes.CreateDecryptor();

        var input = ciphertext.ToArray();

        return decryptor.TransformFinalBlock(
            input,
            0,
            input.Length);
    }

    private static Aes CreateAes()
    {
        var aes = Aes.Create();

        aes.KeySize = 256;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Key;

        return aes;
    }
}
