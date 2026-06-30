namespace TiraxTech.UriTest;

// Verifies the Outcome<Uri> chaining helpers (lift Uri operations onto Outcome<Uri>).
public class OutcomeChainTests
{
    [Test]
    [DisplayName("Builder operations chain directly over Outcome<Uri>")]
    public async Task ChainOverOutcome()
    {
        var result = Uri.Http.Host("example.org")
                         .ChangePath("test/uri")
                         .SetPort(8000)
                         .UpdateQuery(("a", "123"), ("b", "456"))
                         .SetFragment("fragment")
                         .SetCredentials("user", "password");

        await Assert.That(result.Unwrap().ToString())
                    .IsEqualTo("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");
    }

    [Test]
    [DisplayName("A failure short-circuits the rest of the chain")]
    public async Task ChainShortCircuitsOnFailure()
    {
        var result = Uri.Http.Host("example.org")
                         .ChangePath("bad?segment")   // invalid path char -> failure
                         .SetPort(8000)
                         .SetFragment("never");

        await Assert.That(result.IsFail).IsTrue();
        await Assert.That(result.UnwrapError().Code).IsEqualTo(UriError.INVALID_PATH_CHAR);
    }

    [Test]
    [DisplayName("Cached() lifts onto Outcome<Uri>")]
    public async Task CachedLift()
    {
        var cached = Uri.Https.Host("google.com").Cached();
        await Assert.That(cached.Unwrap().Uri).IsEqualTo(Uri.From("https://google.com").Unwrap());
    }
}
