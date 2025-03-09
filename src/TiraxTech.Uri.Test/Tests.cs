using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
using TiraxTech.Json;
using Xunit;

namespace TiraxTech.UriTest;

public class TiraxUriExtensionTests
{
    [Fact(DisplayName="URL without trailing slash, must still be without it after converted back-and-forth")]
    public void UrlWithoutTrailingSlash_RemainsWithoutAfterConversion()
    {
        var uri = Uri.From("http://www.example.org/test");
        var systemUri = uri.ToSystemUri();
        systemUri.AbsoluteUri.Should().Be("http://www.example.org/test");
    }

    [Fact(DisplayName = "Trailing slash must be preserved")]
    public void TrailingSlash_IsPreserved()
    {
        var uri = Uri.From("http://www.example.org/test/");
        var systemUri = uri.ToSystemUri();
        systemUri.AbsoluteUri.Should().Be("http://www.example.org/test/");
    }

    // ---------------------------------------- UPDATE QUERY STRING ----------------------------------------
    static readonly Uri SampleUri = Uri.From("https://www.google.com/search?q=hello&hl=en");

    [Fact(DisplayName = "Remove a single query from URL")]
    public void RemoveSingleQueryFromUrl()
    {
        var result = SampleUri.RemoveQuery("q");
        result.ToString().Should().Be("https://www.google.com/search?hl=en");

        var result2 = result.RemoveQuery("hl");
        result2.ToString().Should().Be("https://www.google.com/search");
    }

    [Fact(DisplayName = "Update query parameters (KeyValue pairs)")]
    public void UpdateQueryParameters_KeyValuePairs()
    {
        var result = SampleUri.UpdateQueries([
            new KeyValuePair<string, StringValues>("f", new StringValues("true")),
            new KeyValuePair<string, StringValues>("hl", new StringValues("world"))
        ]);
        result.ToString().Should().Be("https://www.google.com/search?f=true&hl=en&hl=world&q=hello");
    }

    [Fact(DisplayName = "Update query parameters (tuples)")]
    public void UpdateQueryParameters_Tuples()
    {
        var result = SampleUri.UpdateQueries([
            ("f", new StringValues("true")),
            ("hl", new StringValues("world"))
        ]);
        result.ToString().Should().Be("https://www.google.com/search?f=true&hl=en&hl=world&q=hello");
    }

    // ---------------------------------------- JSON CONVERTER ----------------------------------------
    static JsonSerializerOptions CreateJsonOptions()
        => new() {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { TiraxUriJsonConverter.Instance }
        };

    [Fact(DisplayName = "Convert Uri to JSON")]
    public void ConvertUriToJson()
    {
        var jsonOptions = CreateJsonOptions();
        var result = JsonSerializer.Serialize(SampleUri, jsonOptions);
        result.Should().Be("\"https://www.google.com/search?hl=en&q=hello\"");
    }

    [Fact(DisplayName = "Convert JSON to URI")]
    public void ConvertJsonToUri()
    {
        var jsonOptions = CreateJsonOptions();
        var json = "\"https://www.google.com/search?hl=en&q=hello\"";
        var result = JsonSerializer.Deserialize<Uri>(json, jsonOptions);
        result.Should().Be(SampleUri);
    }

    // ---------------------------------------- RELATIVE URI ----------------------------------------
    [Fact(DisplayName = "Simple relative URI")]
    public void SimpleRelativeUri()
    {
        var result = RelativeUri.From("/search");
        result.Paths.Should().BeEquivalentTo(new[] { "", "search" });
        result.QueryParams.Should().BeEquivalentTo(new KeyValuePair<string, StringValues>[] { });
        result.Fragment.Should().BeNull();
        result.ToString().Should().Be("/search");
    }

    [Fact(DisplayName = "Relative URI with query parameters")]
    public void RelativeUriWithQueryParameters()
    {
        var result = RelativeUri.From("/search?q=hello&hl=en");
        result.Paths.Should().BeEquivalentTo(new[] { "", "search" });
        result.QueryParams.Should().BeEquivalentTo(new[]
        {
            new KeyValuePair<string, StringValues>("q", new StringValues("hello")),
            new KeyValuePair<string, StringValues>("hl", new StringValues("en"))
        });
        result.Fragment.Should().BeNull();
        result.ToString().Should().Be("/search?hl=en&q=hello");
    }

    [Fact(DisplayName = "Relative URI with a fragment")]
    public void RelativeUriWithFragment()
    {
        var result = RelativeUri.From("/search#top");
        result.Paths.Should().BeEquivalentTo(new[] { "", "search" });
        result.QueryParams.Should().BeEquivalentTo(new KeyValuePair<string, StringValues>[] { });
        result.Fragment.Should().Be("top");
        result.ToString().Should().Be("/search#top");
    }

    [Fact(DisplayName = "Relative URI with query parameters and a fragment")]
    public void RelativeUriWithQueryAndFragment()
    {
        var result = RelativeUri.From("/search?q=xxx#top");
        result.Paths.Should().BeEquivalentTo(new[] { "", "search" });
        result.QueryParams.Should().BeEquivalentTo(new[]
        {
            new KeyValuePair<string, StringValues>("q", new StringValues("xxx"))
        });
        result.Fragment.Should().Be("top");
        result.ToString().Should().Be("/search?q=xxx#top");
    }
}