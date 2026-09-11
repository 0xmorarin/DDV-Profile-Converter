using System.Text;
using DDVProfileConverter.Core.Conversion;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileFileConverterTests
{
    [TestMethod]
    public void ConvertInPlaceDecryptsEncryptedProfile()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var encrypted =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        var result =
            ProfileFileConverter.ConvertInPlace(
                profilePath);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            result.InputFormat);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            result.OutputFormat);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        Assert.IsNotNull(
            result.Backup);

        Assert.IsTrue(
            File.Exists(
                result.Backup!.Path));

        var output =
            ProfileReader.Read(
                File.ReadAllBytes(profilePath));

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            output.Format);
    }

    [TestMethod]
    public void ConvertInPlaceEncryptsPlaintextProfile()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        File.WriteAllBytes(
            profilePath,
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson()));

        var result =
            ProfileFileConverter.ConvertInPlace(
                profilePath);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            result.InputFormat);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            result.OutputFormat);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        Assert.IsNull(
            result.Backup);

        var output =
            ProfileReader.Read(
                File.ReadAllBytes(profilePath));

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            output.Format);

        Assert.IsFalse(
            Directory.Exists(
                Path.Combine(
                    directory.Path,
                    "backups")));
    }

    [TestMethod]
    public void ConvertInPlaceCanRoundTripWithTwoCalls()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var encrypted =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        var first =
            ProfileFileConverter.ConvertInPlace(
                profilePath);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            first.OutputFormat);

        var second =
            ProfileFileConverter.ConvertInPlace(
                profilePath);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            second.OutputFormat);

        Assert.IsNull(
            second.Backup);

        var finalResult =
            ProfileReader.Read(
                File.ReadAllBytes(profilePath));

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            finalResult.Format);

        CollectionAssert.AreEqual(
            TestProfileFactory.CreateJson(),
            finalResult.JsonBytes);

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "backups");

        Assert.AreEqual(
            1,
            Directory.GetFiles(
                backupDirectory)
            .Length);
    }

    [TestMethod]
    public void ConvertInPlaceRejectsInvalidInputWithoutModification()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var original =
            Encoding.UTF8.GetBytes(
                "not a DDV profile");

        File.WriteAllBytes(
            profilePath,
            original);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileFileConverter.ConvertInPlace(
                profilePath));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(profilePath));

        Assert.IsFalse(
            Directory.Exists(
                Path.Combine(
                    directory.Path,
                    "backups")));
    }
}
