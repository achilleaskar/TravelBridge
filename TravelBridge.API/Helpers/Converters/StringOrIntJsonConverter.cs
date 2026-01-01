using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelBridge.API.Helpers.Converters
{
    /// <summary>
    /// Handles JSON values that can be either string or int, returning an int.
    /// Used for WebHotelier API responses where min_stay can be "1" or 1.
    /// </summary>
    public class StringOrIntJsonConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.String => int.TryParse(reader.GetString(), out var result) ? result : 0,
                JsonTokenType.Null => 0,
                _ => throw new JsonException($"Unexpected token parsing int. Token: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
