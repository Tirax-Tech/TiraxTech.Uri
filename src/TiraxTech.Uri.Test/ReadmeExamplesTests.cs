using RZ.Foundation;

namespace TiraxTech.UriTest;

// Locks the examples shown in README.md so the documentation cannot silently drift.
public class ReadmeExamplesTests
{
    static Uri BuildExample() =>
        (from u in Uri.Http.Host("example.org")
         from withPath in u.ChangePath("test/uri")
         from full in withPath.SetPort(8000)
                              .UpdateQuery(("a", "123"), ("b", "456"))
                              .SetFragment("fragment")
                              .SetCredentials("user", "password")
         select full).Unwrap();

    [Test]
    public async Task QuickExample()
        => await Assert.That(BuildExample().ToString())
                       .IsEqualTo("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");

    [Test]
    public async Task QueryManipulation()
    {
        var uri2 = BuildExample()
                   .UpdateQuery("c", "789")
                   .RemoveQuery("a")
                   .ReplaceQuery("c", "123")
                   .UpdateQuery("b", "789");
        await Assert.That(uri2.ToString())
                    .IsEqualTo("http://user:password@example.org:8000/test/uri?b=456&b=789&c=123#fragment");
    }

    [Test]
    public async Task ReuseExample()
    {
        var baseApi = Uri.Https.Host("example.org")
                               .Bind(u => u.ChangePath("api/search"))
                               .Map(u => u.UpdateQuery("q", "beer"))
                               .Unwrap();
        var searchWine = baseApi.ReplaceQuery("q", "wine");
        var searchSpecial = searchWine.ChangePath("special").Unwrap();

        await Assert.That(baseApi.ToString()).IsEqualTo("https://example.org/api/search?q=beer");
        await Assert.That(searchWine.ToString()).IsEqualTo("https://example.org/api/search?q=wine");
        await Assert.That(searchSpecial.ToString()).IsEqualTo("https://example.org/api/search/special?q=wine");
    }
}
