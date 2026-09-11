using System.Security.Cryptography;
using DDVProfileConverter.Core.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileCryptoTests
{
    [TestMethod]
    public void EncryptAndDecryptRoundTrip()
    {
        var archive = TestProfileFactory.CreateArchive(
            ("profile", TestProfileFactory.CreateJson()));

        var encrypted = ProfileCrypto.Encrypt(archive);
        var decrypted = ProfileCrypto.Decrypt(encrypted);

        CollectionAssert.AreEqual(archive, decrypted);
    }

    [TestMethod]
    public void DecryptRestoresOriginalArchive()
    {
        var archive = TestProfileFactory.CreateArchive(
            ("profile", TestProfileFactory.CreateJson()));

        var encrypted = TestProfileFactory.Encrypt(archive);

        var decrypted = ProfileCrypto.Decrypt(encrypted);

        CollectionAssert.AreEqual(archive, decrypted);
    }

    [TestMethod]
    public void EncryptRejectsEmptyInput()
    {
        Assert.ThrowsExactly<CryptographicException>(
            () => ProfileCrypto.Encrypt(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecryptRejectsInvalidBlockLength()
    {
        var input = new byte[]
        {
            1,
            2,
            3
        };

        Assert.ThrowsExactly<CryptographicException>(
            () => ProfileCrypto.Decrypt(input));
    }

    [TestMethod]
    public void DecryptRejectsEmptyInput()
    {
        Assert.ThrowsExactly<CryptographicException>(
            () => ProfileCrypto.Decrypt(Array.Empty<byte>()));
    }
}
