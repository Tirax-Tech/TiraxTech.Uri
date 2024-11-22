using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace TiraxTech.Json;

[PublicAPI]
public sealed class TiraxUriJsonConverter : JsonConverter<Uri>
{
    public static readonly TiraxUriJsonConverter Instance = new();

    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var v = reader.GetString();
        return v is null ? null : Uri.From(v);
    }

    public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}