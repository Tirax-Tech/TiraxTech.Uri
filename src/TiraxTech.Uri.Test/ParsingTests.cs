namespace TiraxTech.UriTest;

// Regression tests for path/query parsing defects (#2 %2F, #3 Trim, #4/5 ordering, #11/14/17 empty query).
public class ParsingTests
{
    [Test]
    [DisplayName("Encoded slash %2F stays within a single path segment")]
    public async Task EncodedSlashStaysWithinOneSegment()
    {
        var r = RelativeUri.From("/a%2Fb/c");
        await Assert.That(r.Paths).IsEquivalentTo(new[] { "", "a/b", "c" });
    }

    [Test]
    [DisplayName("Leading/trailing whitespace in a path segment is preserved")]
    public async Task PathSegmentWhitespaceIsPreserved()
    {
        var r = RelativeUri.From("/%20a%20/b");
        await Assert.That(r.Paths).IsEquivalentTo(new[] { "", " a ", "b" });
    }

    [Test]
    [DisplayName("Multi-value query preserves input order deterministically")]
    public async Task MultiValueQueryPreservesInputOrder()
    {
        await Assert.That(Uri.From("http://h/p?a=123&a=456").ToString())
                    .IsEqualTo("http://h/p?a=123&a=456");
        await Assert.That(Uri.From("http://h/p?x=3&x=1&x=2").ToString())
                    .IsEqualTo("http://h/p?x=3&x=1&x=2");
    }

    [Test]
    [DisplayName("Lone '?' yields no query params and round-trips")]
    public async Task LoneQuestionMarkYieldsNoParams()
    {
        var r = RelativeUri.From("/p?");
        await Assert.That(r.QueryParams).IsEmpty();
        await Assert.That(r.ToString()).IsEqualTo("/p");
    }

    [Test]
    [DisplayName("Empty/leading query pairs are dropped (no empty-key, no stray &)")]
    public async Task EmptyQueryPairsAreDropped()
    {
        var r = RelativeUri.From("/p?&a=1");
        await Assert.That(r.QueryParams.ContainsKey("")).IsFalse();
        await Assert.That(r.ToString()).IsEqualTo("/p?a=1");

        var r2 = RelativeUri.From("/p?=v");
        await Assert.That(r2.QueryParams.ContainsKey("")).IsFalse();
    }

    [Test]
    [DisplayName("Valueless 'k' and empty-value 'k=' are distinct and round-trip idempotently")]
    public async Task ValuelessAndEmptyValueKeysRoundTrip()
    {
        await Assert.That(RelativeUri.From("/p?k").ToString()).IsEqualTo("/p?k");
        await Assert.That(RelativeUri.From("/p?k=").ToString()).IsEqualTo("/p?k=");
    }
}
