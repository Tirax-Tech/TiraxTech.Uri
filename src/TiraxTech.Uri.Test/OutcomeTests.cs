using System.Text.Encodings.Web;
using System.Text.Json;
using TiraxTech.Json;

namespace TiraxTech.UriTest;

// Regression tests for the Outcome refactor and its enabled fixes (#13, #10, #7, #15, #16).
public class OutcomeTests
{
    [Test]
    [DisplayName("From returns a failure for malformed input instead of throwing (#13)")]
    public async Task FromFailsOnMalformedInput()
    {
        var result = Uri.From("ht tp://bad");
        await Assert.That(result.IsFail).IsTrue();
        await Assert.That(result.UnwrapError().Code).IsEqualTo(UriError.PARSE);
    }

    [Test]
    [DisplayName("Empty user with a password fails (#10)")]
    public async Task EmptyUserWithPasswordFails()
    {
        var parsed = Uri.From("http://:pwd@host");
        await Assert.That(parsed.IsFail).IsTrue();
        await Assert.That(parsed.UnwrapError().Code).IsEqualTo(UriError.USER_REQUIRED);

        var viaSet = Uri.From("http://host").Unwrap().SetCredentials("", "pwd");
        await Assert.That(viaSet.IsFail).IsTrue();
    }

    [Test]
    [DisplayName("net.pipe with a port fails gracefully instead of throwing (#15)")]
    public async Task NetPipeWithPortFailsGracefully()
    {
        var result = Uri.From("net.pipe://host:808/x");
        await Assert.That(result.IsFail).IsTrue();
    }

    [Test]
    [DisplayName("JSON converter handles null and round-trips a value (#16)")]
    public async Task JsonNullAndRoundTrip()
    {
        var options = new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { TiraxUriJsonConverter.Instance }
        };

        var fromNull = JsonSerializer.Deserialize<Uri?>("null", options);
        await Assert.That(fromNull).IsNull();

        var uri = Uri.From("https://www.google.com/search?q=hello").Unwrap();
        var json = JsonSerializer.Serialize(uri, options);
        var back = JsonSerializer.Deserialize<Uri>(json, options);
        await Assert.That(back).IsEqualTo(uri);
    }

    [Test]
    [DisplayName("ToString renders faithfully; ToSystemUri is a fallible conversion (no sentinel)")]
    public async Task RenderFaithfullyAndConvertFallibly()
    {
        var ok = Uri.From("https://example.com/app").Unwrap();
        await Assert.That(ok.ToString()).IsEqualTo("https://example.com/app");
        await Assert.That(ok.ToSystemUri().IsSuccess).IsTrue();

        // A host mutated via raw `with` is the developer's responsibility: ToString still renders it
        // verbatim (no placeholder/throw), and ToSystemUri reports the conversion failure.
        var badHost = ok with { Host = "bad host" };
        await Assert.That(badHost.ToString()).IsEqualTo("https://bad host/app");
        await Assert.That(badHost.ToSystemUri().IsFail).IsTrue();
    }

    [Test]
    [DisplayName("ToString stays total for a `with`-corrupted scheme or port; ToSystemUri fails")]
    public async Task ToStringIsTotalForCorruptedSchemeAndPort()
    {
        var ok = Uri.From("https://example.com/app").Unwrap();

        // A corrupted scheme makes System.UriBuilder throw, but the custom serializer renders it as-is.
        var badScheme = ok with { Scheme = "ht tp" };
        await Assert.That(badScheme.ToString()).IsEqualTo("ht tp://example.com/app");
        await Assert.That(badScheme.ToSystemUri().IsFail).IsTrue();

        // An out-of-range port renders faithfully too; only the System.Uri conversion fails.
        var badPort = ok with { Port = 999_999 };
        await Assert.That(badPort.ToString()).IsEqualTo("https://example.com:999999/app");
        await Assert.That(badPort.ToSystemUri().IsFail).IsTrue();
    }
}
