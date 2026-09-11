using DDVProfileConverter.Core.Backup;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileBackupFileNameTests
{
    [TestMethod]
    public void BuildProducesExpectedCanonicalName()
    {
        var metadata = new ProfileMetadata(
            591,
            "3768951AEBEA42C5",
            null,
            "2026-07-11T08:11:04.402572100Z",
            "Morarin",
            null,
            null);

        var fileName =
            ProfileBackupFileName.Build(metadata);

        Assert.AreEqual(
            "mdc3768951AEBEA42C5_v591_20260711T081104Z.profile.bak",
            fileName);
    }

    [TestMethod]
    public void BuildAcceptsTimestampWithoutFraction()
    {
        var metadata = new ProfileMetadata(
            608,
            "3768951AEBEA42C5",
            null,
            "2026-09-09T03:52:12Z",
            "Morarin",
            null,
            null);

        var fileName =
            ProfileBackupFileName.Build(metadata);

        Assert.AreEqual(
            "mdc3768951AEBEA42C5_v608_20260909T035212Z.profile.bak",
            fileName);
    }

    [TestMethod]
    public void BuildRejectsMissingOwner()
    {
        var metadata = new ProfileMetadata(
            608,
            null,
            null,
            "2026-09-09T03:52:12.521318300Z",
            null,
            null,
            null);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileBackupFileName.Build(metadata));
    }

    [TestMethod]
    public void BuildRejectsInvalidModified()
    {
        var metadata = new ProfileMetadata(
            608,
            "3768951AEBEA42C5",
            null,
            "not-a-date",
            null,
            null,
            null);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileBackupFileName.Build(metadata));
    }

    [TestMethod]
    public void AddCollisionSuffixPlacesSuffixBeforeExtension()
    {
        const string canonical =
            "mdc3768951AEBEA42C5_v591_20260711T081104Z.profile.bak";

        var fileName =
            ProfileBackupFileName.AddCollisionSuffix(
                canonical,
                2);

        Assert.AreEqual(
            "mdc3768951AEBEA42C5_v591_20260711T081104Z_2.profile.bak",
            fileName);
    }
}
