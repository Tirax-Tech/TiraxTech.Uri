using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TiraxTech.Json;

[PublicAPI]
public sealed class TiraxUriJsonConverter : JsonConverter<Uri>
{
    // ReSharper disable once InconsistentNaming
    public static readonly TiraxUriJsonConverter Instance = new();

    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var v = reader.GetString();
        return v is null ? null : Uri.From(v).Match(u => u, e => throw new JsonException(e.Message));
    }

    public override void Write(Utf8JsonWriter writer, Uri? value, JsonSerializerOptions options) {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}
