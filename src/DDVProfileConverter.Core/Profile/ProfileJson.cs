using System.Buffers;
using System.Text.Json;

namespace DDVProfileConverter.Core.Profile;

public static class ProfileJson
{
    public static bool TryReadMetadata(ReadOnlySpan<byte> json, out ProfileMetadata? metadata)
    {
        try
        {
            metadata = ReadMetadata(json);
            return true;
        }
        catch (JsonException)
        {
            metadata = null;
            return false;
        }
        catch (InvalidDataException)
        {
            metadata = null;
            return false;
        }
    }

    public static ProfileMetadata ReadMetadata(ReadOnlySpan<byte> json)
    {
        using var document = JsonDocument.Parse(json.ToArray());

        return ReadMetadata(document.RootElement);
    }

    public static byte[] PrettyPrint(ReadOnlySpan<byte> json)
    {
        return Rewrite(json, true);
    }

    public static byte[] Minify(ReadOnlySpan<byte> json)
    {
        return Rewrite(json, false);
    }

    private static byte[] Rewrite(ReadOnlySpan<byte> json, bool indented)
    {
        using var document = JsonDocument.Parse(json.ToArray());

        _ = ReadMetadata(document.RootElement);

        var buffer = new ArrayBufferWriter<byte>();

        var options = new JsonWriterOptions
        {
            Indented = indented,
            IndentCharacter = ' ',
            IndentSize = indented ? 4 : 0,
            NewLine = "\n"
        };

        using var writer = new Utf8JsonWriter(buffer, options);

        document.RootElement.WriteTo(writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    private static ProfileMetadata ReadMetadata(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The profile root must be a JSON object.");
        }

        if (!root.TryGetProperty("GameInfo", out var gameInfo) ||
            gameInfo.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("GameInfo is missing or invalid.");
        }

        if (!gameInfo.TryGetProperty("Version", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out var version) ||
            version < 0)
        {
            throw new InvalidDataException("GameInfo.Version is missing or invalid.");
        }

        var lastCustomIdOwner = ReadOptionalString(gameInfo, "LastCustomIdOwner");
        var modified = ReadOptionalString(gameInfo, "Modified");
        var playerName = ReadPlayerName(root);

        return new ProfileMetadata(
            version,
            lastCustomIdOwner,
            modified,
            playerName);
    }

    private static string? ReadOptionalString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{propertyName} is invalid.");
        }

        return element.GetString();
    }

    private static string? ReadPlayerName(JsonElement root)
    {
        if (!root.TryGetProperty("Player", out var player))
        {
            return null;
        }

        if (player.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Player is invalid.");
        }

        return ReadOptionalString(player, "Name");
    }
}
