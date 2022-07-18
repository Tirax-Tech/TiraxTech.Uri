using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LanguageExt.Prelude;

namespace TiraxTech.Json;

public sealed class TiraxUriJsonConverter : JsonConverter<Uri>
{
    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Optional(reader.GetString()!).Map(Uri.From).Match(x => (Uri?) x, () => null);

    public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options) => 
        writer.WriteStringValue(value.ToString());
}