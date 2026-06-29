using System.Text.Encodings.Web;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using TiraxTech.Json;

namespace TiraxTech.UriTest;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class TiraxUriExtensionTests
{
    [Test]
    [DisplayName("URL without trailing slash, must still be without it after converted back-and-forth")]
    public async Task UrlWithoutTrailingSlash_RemainsWithoutAfterConversion()
    {
        var uri = Uri.From("http://www.example.org/test").Unwrap();
        var systemUri = uri.ToSystemUri();
        await Assert.That(systemUri.AbsoluteUri).IsEqualTo("http://www.example.org/test");
    }

    [Test]
    [DisplayName("Trailing slash must be preserved")]
    public async Task TrailingSlash_IsPreserved()
    {
        var uri = Uri.From("http://www.example.org/test/").Unwrap();
        var systemUri = uri.ToSystemUri();
        await Assert.That(systemUri.AbsoluteUri).IsEqualTo("http://www.example.org/test/");
    }

    // ---------------------------------------- UPDATE QUERY STRING ----------------------------------------
    static readonly Uri SampleUri = Uri.From("https://www.google.com/search?q=hello&hl=en").Unwrap();

    [Test]
    [DisplayName("Remove a single query from URL")]
    public async Task RemoveSingleQueryFromUrl()
    {
        var result = SampleUri.RemoveQuery("q");
        await Assert.That(result.ToString()).IsEqualTo("https://www.google.com/search?hl=en");

        var result2 = result.RemoveQuery("hl");
        await Assert.That(result2.ToString()).IsEqualTo("https://www.google.com/search");
    }

    [Test]
    [DisplayName("Update query parameters (KeyValue pairs)")]
    public async Task UpdateQueryParameters_KeyValuePairs()
    {
        var result = SampleUri.UpdateQueries([
            new KeyValuePair<string, StringValues>("f", new StringValues("true")),
            new KeyValuePair<string, StringValues>("hl", new StringValues("world"))
        ]);
        await Assert.That(result.ToString()).IsEqualTo("https://www.google.com/search?f=true&hl=en&hl=world&q=hello");
    }

    [Test]
    [DisplayName("Update query parameters (tuples)")]
    public async Task UpdateQueryParameters_Tuples()
    {
        var result = SampleUri.UpdateQueries([
            ("f", new StringValues("true")),
            ("hl", new StringValues("world"))
        ]);
        await Assert.That(result.ToString()).IsEqualTo("https://www.google.com/search?f=true&hl=en&hl=world&q=hello");
    }

    // ---------------------------------------- JSON CONVERTER ----------------------------------------
    static JsonSerializerOptions CreateJsonOptions()
        => new() {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { TiraxUriJsonConverter.Instance }
        };

    [Test]
    [DisplayName("Convert Uri to JSON")]
    public async Task ConvertUriToJson()
    {
        var jsonOptions = CreateJsonOptions();
        var result = JsonSerializer.Serialize(SampleUri, jsonOptions);
        await Assert.That(result).IsEqualTo("\"https://www.google.com/search?hl=en&q=hello\"");
    }

    [Test]
    [DisplayName("Convert JSON to URI")]
    public async Task ConvertJsonToUri()
    {
        var jsonOptions = CreateJsonOptions();
        var json = "\"https://www.google.com/search?hl=en&q=hello\"";
        var result = JsonSerializer.Deserialize<Uri>(json, jsonOptions);
        await Assert.That(result).IsEqualTo(SampleUri);
    }

    // ---------------------------------------- RELATIVE URI ----------------------------------------
    [Test]
    [DisplayName("Simple relative URI")]
    public async Task SimpleRelativeUri()
    {
        var result = RelativeUri.From("/search");
        await Assert.That(result.Paths).IsEquivalentTo(new[] { "", "search" });
        await Assert.That(result.QueryParams).IsEmpty();
        await Assert.That(result.Fragment).IsNull();
        await Assert.That(result.ToString()).IsEqualTo("/search");
    }

    [Test]
    [DisplayName("Relative URI with query parameters")]
    public async Task RelativeUriWithQueryParameters()
    {
        var result = RelativeUri.From("/search?q=hello&hl=en");
        await Assert.That(result.Paths).IsEquivalentTo(new[] { "", "search" });
        await Assert.That(result.QueryParams).IsEquivalentTo(new[]
        {
            new KeyValuePair<string, StringValues>("q", new StringValues("hello")),
            new KeyValuePair<string, StringValues>("hl", new StringValues("en"))
        });
        await Assert.That(result.Fragment).IsNull();
        await Assert.That(result.ToString()).IsEqualTo("/search?hl=en&q=hello");
    }

    [Test]
    [DisplayName("Relative URI with a fragment")]
    public async Task RelativeUriWithFragment()
    {
        var result = RelativeUri.From("/search#top");
        await Assert.That(result.Paths).IsEquivalentTo(new[] { "", "search" });
        await Assert.That(result.QueryParams).IsEmpty();
        await Assert.That(result.Fragment).IsEqualTo("top");
        await Assert.That(result.ToString()).IsEqualTo("/search#top");
    }

    [Test]
    [DisplayName("Relative URI with query parameters and a fragment")]
    public async Task RelativeUriWithQueryAndFragment()
    {
        var result = RelativeUri.From("/search?q=xxx#top");
        await Assert.That(result.Paths).IsEquivalentTo(new[] { "", "search" });
        await Assert.That(result.QueryParams).IsEquivalentTo(new[]
        {
            new KeyValuePair<string, StringValues>("q", new StringValues("xxx"))
        });
        await Assert.That(result.Fragment).IsEqualTo("top");
        await Assert.That(result.ToString()).IsEqualTo("/search?q=xxx#top");
    }
}
