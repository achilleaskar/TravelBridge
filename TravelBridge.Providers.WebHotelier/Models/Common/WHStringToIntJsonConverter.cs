namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// Converter that handles int values that may come as strings or empty strings from the API.
/// Returns 0 for null or empty string values.
/// </summary>
public class WHStringToIntJsonConverter : JsonConverter<int>
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
