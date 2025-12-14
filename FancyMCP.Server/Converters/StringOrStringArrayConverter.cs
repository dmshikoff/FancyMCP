using System.Text.Json;
using System.Text.Json.Serialization;

namespace FancyMCP.Server.Converters;

/// <summary>
/// JSON converter that handles properties that can be either a single string or an array of strings.
/// Converts single strings to single-element arrays and handles null values gracefully.
/// </summary>
public class StringOrStringArrayConverter : JsonConverter<string[]?>
{
    public override string[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            
            case JsonTokenType.String:
                // Single string value - convert to array with one element
                string? singleValue = reader.GetString();
                return singleValue != null ? new[] { singleValue } : null;
            
            case JsonTokenType.StartArray:
                // Array of strings - deserialize normally
                List<string> list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return list.ToArray();
                    }
                    
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        string? value = reader.GetString();
                        if (value != null)
                        {
                            list.Add(value);
                        }
                    }
                }
                return list.ToArray();
            
            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}. Expected String or StartArray.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.Length == 1)
        {
            // Write single value as string (not array)
            writer.WriteStringValue(value[0]);
        }
        else
        {
            // Write multiple values as array
            writer.WriteStartArray();
            foreach (string item in value)
            {
                writer.WriteStringValue(item);
            }
            writer.WriteEndArray();
        }
    }
}
