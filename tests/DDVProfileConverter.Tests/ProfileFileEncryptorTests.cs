using System.Text;
using DDVProfileConverter.Core.Conversion;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileFileEncryptorTests
{
    [TestMethod]
    public void EncryptInPlaceProducesEncryptedProfile()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var plaintext =
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson());

        File.WriteAllBytes(
            profilePath,
            plaintext);

        var result =
            ProfileFileEncryptor.EncryptInPlace(
                profilePath);

        var output =
            File.ReadAllBytes(profilePath);

        var readResult =
            ProfileReader.Read(output);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            readResult.Format);

        Assert.AreEqual(
            608,
            result.Metadata.Version);

        Assert.AreEqual(
            "0123456789ABCDEF",
            result.Metadata.LastCustomIdOwner);

        CollectionAssert.AreEqual(
            TestProfileFactory.CreateJson(),
            readResult.JsonBytes);

        Assert.IsFalse(
            Directory.Exists(
                Path.Combine(
                    directory.Path,
                    "backups")));

        var tempFiles =
            Directory.GetFiles(
                directory.Path,
                ".*.tmp");

        Assert.AreEqual(
            0,
            tempFiles.Length);
    }

    [TestMethod]
    public void EncryptInPlaceRejectsEncryptedWithoutModification()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var original =
            ProfileWriter.Encrypt(
                TestProfileFactory.CreateJson())
            .EncryptedBytes;

        File.WriteAllBytes(
            profilePath,
            original);

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileFileEncryptor.EncryptInPlace(
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

    [TestMethod]
    public void EncryptInPlaceRejectsInvalidInputWithoutModification()
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
            () => ProfileFileEncryptor.EncryptInPlace(
                profilePath));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(profilePath));
    }

    [TestMethod]
    public void EncryptInPlaceMinifiesJsonBeforeEncryption()
    {
        using var directory =
            new TemporaryDirectory();

        var profilePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var pretty =
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson());

        File.WriteAllBytes(
            profilePath,
            pretty);

        ProfileFileEncryptor.EncryptInPlace(
            profilePath);

        var readResult =
            ProfileReader.Read(
                File.ReadAllBytes(profilePath));

        CollectionAssert.AreEqual(
            TestProfileFactory.CreateJson(),
            readResult.JsonBytes);
    }

    [TestMethod]
    public void EncryptInPlaceSupportsUnicodeAndSpacesInPath()
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

        File.WriteAllBytes(
            profilePath,
            ProfileJson.PrettyPrint(
                TestProfileFactory.CreateJson()));

        var result =
            ProfileFileEncryptor.EncryptInPlace(
                profilePath);

        Assert.AreEqual(
            Path.GetFullPath(profilePath),
            result.ProfilePath);

        Assert.AreEqual(
            ProfileFormat.Encrypted,
            ProfileReader.Read(
                File.ReadAllBytes(profilePath))
            .Format);
    }
}
