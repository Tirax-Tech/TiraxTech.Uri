namespace Tests

open System.Collections.Generic
open System.Text.Encodings.Web
open System.Text.Json
open FluentAssertions
open Microsoft.Extensions.Primitives
open TiraxTech
open TiraxTech.Json
open Xunit

type Uri = TiraxTech.Uri

type ``TiraxUri extension``() =
    let SampleUri = Uri.From "https://www.google.com/search?q=hello&hl=en"

    [<Fact>]
    let ``Remove a single query from URL`` () =
        let result = SampleUri.RemoveQuery "q"

        result.ToString().Should().Be("https://www.google.com/search?hl=en") |> ignore

        let result2 = result.RemoveQuery "hl"

        result2.ToString().Should().Be("https://www.google.com/search") |> ignore

    [<Fact>]
    let ``Update query parameters (KeyValue pairs)``() =
        let result = SampleUri.UpdateQueries [ KeyValuePair.Create("f", StringValues "true")
                                               KeyValuePair.Create("hl", StringValues "world") ]

        result.ToString().Should().Be("https://www.google.com/search?f=true&hl=en&hl=world&q=hello") |> ignore

    [<Fact>]
    let ``Update query parameters (tuples)``() =
        let result = SampleUri.UpdateQueries [ struct("f", StringValues "true")
                                               ("hl", StringValues "world") ]

        result.ToString().Should().Be("https://www.google.com/search?f=true&hl=en&hl=world&q=hello") |> ignore

    // ---------------------------------------- JSON CONVERTER ----------------------------------------
    let createJsonOptions() =
        let json_options = JsonSerializerOptions()
        json_options.Encoder <- JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        json_options.Converters.Add(TiraxUriJsonConverter.Instance)
        json_options

    let json_options = createJsonOptions()

    [<Fact>]
    let ``Convert Uri to JSON``() =
        let result = JsonSerializer.Serialize(SampleUri, json_options)

        result.Should().Be("\"https://www.google.com/search?hl=en&q=hello\"") |> ignore

    [<Fact>]
    let ``Convert JSON to URI``() =
        let json = "\"https://www.google.com/search?hl=en&q=hello\""

        let result = JsonSerializer.Deserialize<Uri>(json, json_options)

        result.Should().Be(SampleUri) |> ignore
