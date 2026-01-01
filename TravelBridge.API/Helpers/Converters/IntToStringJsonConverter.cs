using System.Text.Json.Serialization;
using System.Text.Json;

namespace TravelBridge.API.Helpers.Converters
{
    public class IntToStringJsonConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle numbers and strings
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64().ToString(),
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException($"Unexpected token parsing string. Token: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
