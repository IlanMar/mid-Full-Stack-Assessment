using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinancialMonitor;

public sealed class UtcTimestampJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            reader.GetString() is { } value &&
            (value.EndsWith('Z') || value.EndsWith("+00:00", StringComparison.Ordinal)) &&
            reader.TryGetDateTimeOffset(out var timestamp))
        {
            return timestamp;
        }

        throw new JsonException("Use a UTC timestamp ending in Z or +00:00.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
