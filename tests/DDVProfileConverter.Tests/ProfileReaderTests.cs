using System.Text;
using DDVProfileConverter.Core.Conversion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileReaderTests
{
    [TestMethod]
    public void ReadDetectsPlainJson()
    {
        var input = TestProfileFactory.CreateJson();

        var result = ProfileReader.Read(input);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            result.Format);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        Assert.AreEqual(
            "0123456789ABCDEF",
            result.Metadata.LastCustomIdOwner);

        CollectionAssert.AreEqual(
            input,
            result.JsonBytes);
    }

    [TestMethod]
    public void ReadDetectsEncryptedProfile()
    {
        var encrypted =
            TestProfileFactory.CreateEncryptedProfile();

        var result = ProfileReader.Read(encrypted);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            result.Format);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        Assert.AreEqual(
            "TestPlayer",
            result.Metadata.PlayerName);

        CollectionAssert.AreEqual(
            TestProfileFactory.CreateJson(),
            result.JsonBytes);
    }

    [TestMethod]
    public void ReadRejectsInvalidInput()
    {
        var input = Encoding.UTF8.GetBytes(
            "not a profile");

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileReader.Read(input));
    }
}
