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

type private KV = KeyValuePair<string, StringValues>

[<AutoOpen>]
module private Sample =
    let [<Literal>] SimpleUri = "http://www.example.org";
    let [<Literal>] SimpleUriFormatted = "http://www.example.org/";

type ``TiraxUri extension``() =
    // ---------------------------------------- SIMPLE CONVERSIONS ----------------------------------------
    [<Fact>]
    let ``URL without trailing slash, must still be without it after converted back-and-forth``() =
        let uri = Uri.From "http://www.example.org/test"

        let system_uri = uri.ToSystemUri()

        system_uri.AbsoluteUri.Should().Be("http://www.example.org/test") |> ignore

    [<Fact>]
    let ``Trailing slash must be preserved``() =
        let uri = Uri.From "http://www.example.org/test/"

        let system_uri = uri.ToSystemUri()

        system_uri.AbsoluteUri.Should().Be("http://www.example.org/test/") |> ignore

    // ---------------------------------------- UPDATE QUERY STRING ----------------------------------------
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

    // ---------------------------------------- RELATIVE URI ----------------------------------------
    [<Fact>]
    let ``Simple relative URI``() =
        let result = RelativeUri.From "/search"

        result.Paths.Should().BeEquivalentTo([""; "search"]) |> ignore
        result.QueryParams.Should<KV>().BeEquivalentTo([]) |> ignore
        result.Fragment.Should().BeNull() |> ignore

        result.ToString().Should().Be("/search") |> ignore

    [<Fact>]
    let ``Relative URI with query parameters``() =
        let result = RelativeUri.From "/search?q=hello&hl=en"

        result.Paths.Should().BeEquivalentTo([""; "search"]) |> ignore
        result.QueryParams.Should<KV>().BeEquivalentTo([ KeyValuePair.Create("q", StringValues "hello")
                                                         KeyValuePair.Create("hl",StringValues "en") ]) |> ignore
        result.Fragment.Should().BeNull() |> ignore

        result.ToString().Should().Be("/search?hl=en&q=hello") |> ignore

    [<Fact>]
    let ``Relative URI with a fragment``() =
        let result = RelativeUri.From "/search#top"

        result.Paths.Should().BeEquivalentTo([""; "search"]) |> ignore
        result.QueryParams.Should<KV>().BeEquivalentTo([]) |> ignore
        result.Fragment.Should().Be("top") |> ignore

        result.ToString().Should().Be("/search#top") |> ignore

    [<Fact>]
    let ``Relative URI with query parameters and a fragment``() =
        let result = RelativeUri.From "/search?q=xxx#top"

        result.Paths.Should().BeEquivalentTo([""; "search"]) |> ignore
        result.QueryParams.Should<KV>().BeEquivalentTo([ KeyValuePair.Create("q", StringValues "xxx") ]) |> ignore
        result.Fragment.Should().Be("top") |> ignore

        result.ToString().Should().Be("/search?q=xxx#top") |> ignore