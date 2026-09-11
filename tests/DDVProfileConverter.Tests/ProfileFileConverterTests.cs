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
                "save",
                "profile.json");

        Directory.CreateDirectory(
            Path.GetDirectoryName(profilePath)!);

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "application",
                "backups");

        var encrypted =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        var result =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

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

        Assert.AreEqual(
            Path.GetFullPath(backupDirectory),
            Path.GetDirectoryName(result.Backup.Path));

        Assert.IsFalse(
            Directory.Exists(
                Path.Combine(
                    Path.GetDirectoryName(profilePath)!,
                    "backups")));

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

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "application",
                "backups");

        File.WriteAllBytes(
            profilePath,
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson()));

        var result =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

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
            Directory.Exists(backupDirectory));
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

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "application",
                "backups");

        var encrypted =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        var first =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            first.OutputFormat);

        var second =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

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

        Assert.AreEqual(
            1,
            Directory.GetFiles(
                backupDirectory)
            .Length);
    }

    [TestMethod]
    public void ConvertInPlaceDoesNotDuplicateBackupAfterReencryptingSameProfile()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "application",
                "backups");

        var compactJson =
            TestProfileFactory.CreateJson();

        var prettyJson =
            ProfileJson.PrettyPrint(compactJson);

        var originalEncrypted =
            TestProfileFactory.Encrypt(
                TestProfileFactory.CreateArchive(
                    ("profile", prettyJson)));

        File.WriteAllBytes(
            profilePath,
            originalEncrypted);

        var first =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

        Assert.IsNotNull(first.Backup);
        Assert.IsTrue(first.Backup!.Created);

        var second =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            second.OutputFormat);

        var reencrypted =
            File.ReadAllBytes(profilePath);

        Assert.IsFalse(
            originalEncrypted
                .AsSpan()
                .SequenceEqual(reencrypted));

        var third =
            ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory);

        Assert.IsNotNull(third.Backup);
        Assert.IsFalse(third.Backup!.Created);

        Assert.AreEqual(
            first.Backup.Path,
            third.Backup.Path);

        CollectionAssert.AreEqual(
            originalEncrypted,
            File.ReadAllBytes(first.Backup.Path));

        Assert.AreEqual(
            1,
            Directory.GetFiles(
                backupDirectory,
                "*.profile.bak")
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

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "application",
                "backups");

        var original =
            Encoding.UTF8.GetBytes(
                "not a DDV profile");

        File.WriteAllBytes(
            profilePath,
            original);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileFileConverter.ConvertInPlace(
                profilePath,
                backupDirectory));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(profilePath));

        Assert.IsFalse(
            Directory.Exists(backupDirectory));
    }
}
