using System.Text;
using DDVProfileConverter.Core.Backup;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileBackupManagerTests
{
    [TestMethod]
    public void CreateWritesExactOriginalBytes()
    {
        using var directory =
            new TemporaryDirectory();

        var sourcePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var original =
            Encoding.UTF8.GetBytes(
                "encrypted-original");

        var result =
            ProfileBackupManager.Create(
                sourcePath,
                original,
                CreateMetadata());

        Assert.IsTrue(result.Created);
        Assert.IsTrue(File.Exists(result.Path));

        CollectionAssert.AreEqual(
            original,
            File.ReadAllBytes(result.Path));
    }

    [TestMethod]
    public void CreateDeduplicatesIdenticalBackup()
    {
        using var directory =
            new TemporaryDirectory();

        var sourcePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var original =
            Encoding.UTF8.GetBytes(
                "encrypted-original");

        var metadata =
            CreateMetadata();

        var first =
            ProfileBackupManager.Create(
                sourcePath,
                original,
                metadata);

        var second =
            ProfileBackupManager.Create(
                sourcePath,
                original,
                metadata);

        Assert.IsTrue(first.Created);
        Assert.IsFalse(second.Created);

        Assert.AreEqual(
            first.Path,
            second.Path);
    }

    [TestMethod]
    public void CreateUsesSuffixForDifferentBytes()
    {
        using var directory =
            new TemporaryDirectory();

        var sourcePath =
            Path.Combine(
                directory.Path,
                "profile.json");

        var metadata =
            CreateMetadata();

        var first =
            ProfileBackupManager.Create(
                sourcePath,
                Encoding.UTF8.GetBytes(
                    "encrypted-a"),
                metadata);

        var second =
            ProfileBackupManager.Create(
                sourcePath,
                Encoding.UTF8.GetBytes(
                    "encrypted-b"),
                metadata);

        Assert.IsTrue(first.Created);
        Assert.IsTrue(second.Created);

        Assert.AreEqual(
            "mdc3768951AEBEA42C5_v591_20260711T081104Z.profile.bak",
            Path.GetFileName(first.Path));

        Assert.AreEqual(
            "mdc3768951AEBEA42C5_v591_20260711T081104Z_2.profile.bak",
            Path.GetFileName(second.Path));
    }

    [TestMethod]
    public void CreatePlacesBackupsBesideSourceDirectory()
    {
        using var directory =
            new TemporaryDirectory();

        var sourceDirectory =
            Path.Combine(
                directory.Path,
                "save");

        Directory.CreateDirectory(
            sourceDirectory);

        var sourcePath =
            Path.Combine(
                sourceDirectory,
                "profile.json");

        var result =
            ProfileBackupManager.Create(
                sourcePath,
                Encoding.UTF8.GetBytes(
                    "encrypted-original"),
                CreateMetadata());

        Assert.AreEqual(
            Path.Combine(
                sourceDirectory,
                "backups"),
            Path.GetDirectoryName(result.Path));
    }

    private static ProfileMetadata CreateMetadata()
    {
        return new ProfileMetadata(
            591,
            "3768951AEBEA42C5",
            "2026-07-11T08:11:04.402572100Z",
            "Morarin");
    }
}
