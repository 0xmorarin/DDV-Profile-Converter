using System.Text;
using DDVProfileConverter.Core.Profile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDVProfileConverter.Tests;

[TestClass]
public sealed class ProfileJsonTests
{
    [TestMethod]
    public void ReadMetadataExtractsExpectedValues()
    {
        var metadata = ProfileJson.ReadMetadata(
            TestProfileFactory.CreateJson());

        Assert.AreEqual(608, metadata.Version);
        Assert.AreEqual(
            "0123456789ABCDEF",
            metadata.LastCustomIdOwner);
        Assert.AreEqual(
            "2025-10-14T23:31:58.070966900Z",
            metadata.Created);
        Assert.AreEqual(
            "2026-09-11T00:00:00.123456700Z",
            metadata.Modified);
        Assert.AreEqual(
            "TestPlayer",
            metadata.PlayerName);
        Assert.AreEqual(
            18867L,
            metadata.TimePlayedInMinutes);
        Assert.AreEqual(
            "DeviceType_Windows",
            metadata.LastSaveDeviceType);
    }

    [TestMethod]
    public void ReadMetadataRejectsMissingGameInfo()
    {
        var json = Encoding.UTF8.GetBytes(
            "{\"Player\":{\"Name\":\"TestPlayer\"}}");

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileJson.ReadMetadata(json));
    }

    [TestMethod]
    public void ReadMetadataRejectsInvalidVersion()
    {
        var json = Encoding.UTF8.GetBytes(
            "{\"GameInfo\":{\"Version\":\"608\"}}");

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileJson.ReadMetadata(json));
    }

    [TestMethod]
    public void ReadMetadataRejectsInvalidPlayTime()
    {
        var json = Encoding.UTF8.GetBytes(
            "{\"GameInfo\":{\"Version\":608},\"Player\":{\"TimePlayedInMinutes\":-1}}");

        Assert.ThrowsExactly<InvalidDataException>(
            () => ProfileJson.ReadMetadata(json));
    }

    [TestMethod]
    public void TryReadMetadataRejectsArbitraryJson()
    {
        var json = Encoding.UTF8.GetBytes(
            "{\"Value\":123}");

        var result = ProfileJson.TryReadMetadata(
            json,
            out var metadata);

        Assert.IsFalse(result);
        Assert.IsNull(metadata);
    }

    [TestMethod]
    public void PrettyPrintUsesFourSpaceIndentation()
    {
        var prettyBytes = ProfileJson.PrettyPrint(
            TestProfileFactory.CreateJson());

        var pretty = Encoding.UTF8.GetString(prettyBytes);

        StringAssert.Contains(
            pretty,
            "\n    \"GameInfo\": {");

        Assert.IsFalse(pretty.Contains('\r'));
    }

    [TestMethod]
    public void MinifyRestoresCompactJson()
    {
        var pretty = ProfileJson.PrettyPrint(
            TestProfileFactory.CreateJson());

        var minified = ProfileJson.Minify(pretty);

        Assert.AreEqual(
            TestProfileFactory.JsonText,
            Encoding.UTF8.GetString(minified));
    }
}
