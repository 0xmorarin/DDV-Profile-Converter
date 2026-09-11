using System.Text;
using DDVProfileConverter.Core.Archive;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileArchiveTests
{
    [TestMethod]
    public void CreateProfileArchiveRoundTrips()
    {
        var expected = TestProfileFactory.CreateJson();

        var archive =
            ProfileArchive.CreateProfileArchive(expected);

        var actual =
            ProfileArchive.ExtractProfile(archive);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ExtractProfileReturnsExpectedBytes()
    {
        var expected = TestProfileFactory.CreateJson();

        var archive = TestProfileFactory.CreateArchive(
            ("profile", expected));

        var actual = ProfileArchive.ExtractProfile(archive);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ExtractProfileRejectsMissingProfileEntry()
    {
        var archive = TestProfileFactory.CreateArchive(
            ("other", Encoding.UTF8.GetBytes("data")));

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileArchive.ExtractProfile(archive));
    }

    [TestMethod]
    public void ExtractProfileRejectsDuplicateProfileEntries()
    {
        var json = TestProfileFactory.CreateJson();

        var archive = TestProfileFactory.CreateArchive(
            ("profile", json),
            ("profile", json));

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileArchive.ExtractProfile(archive));
    }
}
