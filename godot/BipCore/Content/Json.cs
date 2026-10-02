using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BipCore;

/// <summary>
/// The JSON settings for content and saves. Keys are camelCase, like the Swift app's Codable
/// output, so saves written by the Swift app load here unchanged. Missing optional values are left
/// out when writing, as Swift does.
/// </summary>
public static class BipJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    public static T Decode<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException($"Expected a {typeof(T).Name}, found null");

    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
}

/// <summary>An age range, written "4-6" in the content.</summary>
internal sealed class AgeRangeConverter : JsonConverter<AgeRange>
{
    public override AgeRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? "";
        var parts = text.Split('-').Select(p => int.TryParse(p.Trim(), out var n) ? (int?)n : null).ToList();
        if (parts.Count != 2 || parts[0] is not int youngest || parts[1] is not int oldest || youngest > oldest)
        {
            throw new JsonException($"Age range '{text}' should look like 4-6");
        }
        return new AgeRange(youngest, oldest);
    }

    public override void Write(Utf8JsonWriter writer, AgeRange value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"{value.Youngest}-{value.Oldest}");
}

/// <summary>A grid cell, written [row, column] in the content.</summary>
internal sealed class GridPositionConverter : JsonConverter<GridPosition>
{
    public override GridPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<int[]>(ref reader, options);
        if (values is not { Length: 2 }) throw new JsonException("A grid position is [row, column]");
        return new GridPosition(values[0], values[1]);
    }

    public override void Write(Utf8JsonWriter writer, GridPosition value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Row);
        writer.WriteNumberValue(value.Column);
        writer.WriteEndArray();
    }
}

/// <summary>One block repeated, written ["up", 3] in the content.</summary>
internal sealed class RepeatStepConverter : JsonConverter<RepeatStep>
{
    public override RepeatStep Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("A repeat step is [block, times]");
        reader.Read();
        var block = reader.TokenType == JsonTokenType.String ? reader.GetString()! : throw new JsonException("A repeat step is [block, times]");
        reader.Read();
        var times = reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : throw new JsonException("A repeat step is [block, times]");
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("A repeat step is [block, times]");
        return new RepeatStep(block, times);
    }

    public override void Write(Utf8JsonWriter writer, RepeatStep value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Block);
        writer.WriteNumberValue(value.Times);
        writer.WriteEndArray();
    }
}

/// <summary>
/// A moment in time as the Swift app saves it: seconds since 1 January 2001 (UTC), a plain number.
/// </summary>
internal sealed class ReferenceDateConverter : JsonConverter<DateTimeOffset>
{
    public static readonly DateTimeOffset ReferenceDate = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReferenceDate.AddSeconds(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteNumberValue((value - ReferenceDate).TotalSeconds);
}
