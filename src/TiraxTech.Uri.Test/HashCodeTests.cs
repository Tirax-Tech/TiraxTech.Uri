namespace TiraxTech.UriTest;

// Regression tests for the GetHashCode/Equals contract (defect #1).
public class HashCodeTests
{
    [Test]
    [DisplayName("Value-equal Uris produce equal hash codes")]
    public async Task EqualUrisHaveEqualHashCodes()
    {
        var a = Uri.From("http://example.org/a/b?x=1#frag").Unwrap();
        var b = Uri.From("http://example.org/a/b?x=1#frag").Unwrap();

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    [DisplayName("Value-equal RelativeUris produce equal hash codes")]
    public async Task EqualRelativeUrisHaveEqualHashCodes()
    {
        var a = RelativeUri.From("/a/b?x=1&x=2#frag");
        var b = RelativeUri.From("/a/b?x=1&x=2#frag");

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    [DisplayName("Uri works as a HashSet and Dictionary key")]
    public async Task UriWorksAsHashKey()
    {
        var a = Uri.From("http://example.org/a/b?x=1#frag").Unwrap();
        var b = Uri.From("http://example.org/a/b?x=1#frag").Unwrap();

        var set = new HashSet<Uri> { a };
        await Assert.That(set.Contains(b)).IsTrue();

        var dict = new Dictionary<Uri, int> { [a] = 42 };
        await Assert.That(dict.TryGetValue(b, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }
}
