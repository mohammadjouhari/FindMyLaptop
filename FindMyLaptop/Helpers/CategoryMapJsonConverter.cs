namespace FindMyLaptop.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class CategoryMapJsonConverter : JsonConverter<CategoryMap>
    {
        public override CategoryMap? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 1. If Algolia returns an empty array [], return an empty CategoryMap
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                using var doc = JsonDocument.ParseValue(ref reader); // Read the array to advance the reader
                return new CategoryMap();
            }

            // 2. If it's null, return null
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // 3. If it's a JSON object {}, deserialize normally
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var categoryMap = new CategoryMap();
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (root.TryGetProperty("level0", out var level0Prop) && level0Prop.ValueKind == JsonValueKind.Array)
                {
                    categoryMap.Level0 = JsonSerializer.Deserialize<List<string>>(level0Prop.GetRawText(), options);
                }

                if (root.TryGetProperty("level1", out var level1Prop) && level1Prop.ValueKind == JsonValueKind.Array)
                {
                    categoryMap.Level1 = JsonSerializer.Deserialize<List<string>>(level1Prop.GetRawText(), options);
                }

                return categoryMap;
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing CategoryMap.");
        }

        public override void Write(Utf8JsonWriter writer, CategoryMap value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
