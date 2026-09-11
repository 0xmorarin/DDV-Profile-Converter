using DDVProfileConverter.Core.Conversion;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileWriterTests
{
    [TestMethod]
    public void EncryptProducesReadableEncryptedProfile()
    {
        var prettyJson =
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson());

        var written =
            ProfileWriter.Encrypt(prettyJson);

        var read =
            ProfileReader.Read(written.EncryptedBytes);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            read.Format);

        Assert.AreEqual(
            608,
            written.Metadata.Version);

        Assert.AreEqual(
            "0123456789ABCDEF",
            written.Metadata.LastCustomIdOwner);

        CollectionAssert.AreEqual(
            TestProfileFactory.CreateJson(),
            read.JsonBytes);
    }

    [TestMethod]
    public void EncryptProducesBlockAlignedCiphertext()
    {
        var written =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson());

        Assert.IsTrue(
            written.EncryptedBytes.Length > 0);

        Assert.AreEqual(
            0,
            written.EncryptedBytes.Length % 16);
    }

    [TestMethod]
    public void EncryptRejectsInvalidProfileJson()
    {
        var invalidJson =
            System.Text.Encoding.UTF8.GetBytes(
                "{\"Value\":123}");

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileWriter.Encrypt(invalidJson));
    }
}
