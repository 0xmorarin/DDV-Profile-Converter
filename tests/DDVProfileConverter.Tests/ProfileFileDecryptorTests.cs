using System.Text;
using DDVProfileConverter.Core.Conversion;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileFileDecryptorTests
{
    [TestMethod]
    public void DecryptInPlaceCreatesBackupAndPrettyJson()
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
            ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory);

        var output =
            File.ReadAllBytes(profilePath);

        var expected =
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson());

        CollectionAssert.AreEqual(
            expected,
            output);

        CollectionAssert.AreEqual(
            encrypted,
            File.ReadAllBytes(result.Backup.Path));

        Assert.IsTrue(result.Backup.Created);

        Assert.AreEqual(
            Path.GetFullPath(backupDirectory),
            Path.GetDirectoryName(result.Backup.Path));

        Assert.IsFalse(
            Directory.Exists(
                Path.Combine(
                    Path.GetDirectoryName(profilePath)!,
                    "backups")));

        var readResult =
            ProfileReader.Read(output);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            readResult.Format);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        var sourceTempFiles =
            Directory.GetFiles(
                Path.GetDirectoryName(profilePath)!,
                ".*.tmp");

        Assert.AreEqual(
            0,
            sourceTempFiles.Length);
    }

    [TestMethod]
    public void DecryptInPlaceRejectsPlaintextWithoutModification()
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
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson());

        File.WriteAllBytes(
            profilePath,
            original);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(profilePath));

        Assert.IsFalse(
            Directory.Exists(backupDirectory));
    }

    [TestMethod]
    public void DecryptInPlaceRejectsInvalidInputWithoutModification()
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
            () => ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(profilePath));

        Assert.IsFalse(
            Directory.Exists(backupDirectory));
    }

    [TestMethod]
    public void DecryptInPlacePreservesSourceWhenBackupNamingFails()
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

        var json =
            Encoding.UTF8.GetBytes(
                "{\"GameInfo\":{\"Version\":608,\"Modified\":\"2026-09-11T00:00:00.123456700Z\"},\"Player\":{\"Name\":\"TestPlayer\"}}");

        var encrypted =
            ProfileWriter.Encrypt(json)
                .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory));

        CollectionAssert.AreEqual(
            encrypted,
            File.ReadAllBytes(profilePath));

        Assert.IsFalse(
            Directory.Exists(backupDirectory));
    }

    [TestMethod]
    public void DecryptInPlaceSupportsUnicodeAndSpacesInPath()
    {
        using var directory =
            new TemporaryDirectory();

        var saveDirectory =
            Path.Combine(
                directory.Path,
                "セーブ データ");

        Directory.CreateDirectory(
            saveDirectory);

        var profilePath =
            Path.Combine(
                saveDirectory,
                "profile.json");

        var backupDirectory =
            Path.Combine(
                directory.Path,
                "アプリ ケーション",
                "backups");

        var encrypted =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            encrypted);

        var result =
            ProfileFileDecryptor.DecryptInPlace(
                profilePath,
                backupDirectory);

        Assert.AreEqual(
            Path.GetFullPath(profilePath),
            result.ProfilePath);

        Assert.AreEqual(
            ProfileFormat.PlainJson,
            ProfileReader.Read(
                File.ReadAllBytes(profilePath))
            .Format);

        Assert.IsTrue(
            File.Exists(result.Backup.Path));

        Assert.AreEqual(
            Path.GetFullPath(backupDirectory),
            Path.GetDirectoryName(result.Backup.Path));
    }
}
