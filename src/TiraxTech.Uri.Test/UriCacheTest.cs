namespace TiraxTech.UriTest;

public sealed class UriCacheTest
{
    [Test]
    public async Task EqualityTests(){
        const string testUri = "http://example.org/test";
        var uri = UriCache.From(testUri);
        var uri2 = UriCache.From(testUri);
        var uri3 = Uri.From(testUri);

        await Assert.That(uri).IsEqualTo(uri2);
        await Assert.That(uri.Equals(new System.Uri(testUri))).IsTrue();
        await Assert.That(uri.Equals(testUri)).IsTrue();
        await Assert.That(uri).IsEqualTo(uri3.Cached());

        await Assert.That(uri.SystemUri).IsEqualTo(new System.Uri(testUri));
        await Assert.That(uri.Uri).IsEqualTo(uri2.Uri);
    }
}
