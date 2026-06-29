namespace TiraxTech.UriTest;

public class UriBuilderTest
{
    const string SimpleUri = "http://www.example.org";
    const string SimpleUriFormatted = "http://www.example.org/";

    [Test]
    [Arguments(SimpleUri, SimpleUriFormatted)]
    [Arguments("https://example.org", "https://example.org/")]
    [Arguments("ftp://example.org/path", "ftp://example.org/path")]
    [Arguments("ws://example.org/service", "ws://example.org/service")]
    [Arguments("ldap://example.org/service", "ldap://example.org/service")]
    [Arguments("net.tcp://example.org/service", "net.tcp://example.org/service")]
    [Arguments("net.pipe://example.org/service", "net.pipe://example.org/service")]
    public async Task ConvertBetweenStringAndUri(string originUri, string formattedUri)
    {
        Uri uri = originUri;
        var uri1 = Uri.From(originUri);

        await Assert.That(uri).IsEqualTo(uri1);
        await Assert.That(uri.ToString()).IsEqualTo(formattedUri);
    }

    [Test]
    public async Task ChangePortAndFragmentAltogether(){
        Uri uri = SimpleUri;

        await Assert.That((uri with { Port = 123, Path = uri.Path with { Fragment = "test"}}).ToString()).IsEqualTo("http://www.example.org:123/#test");
        await Assert.That(uri.ToString()).IsEqualTo(SimpleUriFormatted);
    }

    #region Path tests

    [Test]
    public async Task ChangeRelativePath(){
        Uri uri = SimpleUri;
        uri = uri.ChangePath("test");
        await Assert.That((uri with { Path = uri.Path with { Fragment = "anchor"}}).ToString()).IsEqualTo("http://www.example.org/test#anchor");
    }

    [Test]
    public async Task ChainChangeRelativePath(){
        Uri uri = SimpleUri;
        uri = uri.ChangePath("test").ChangePath("uri").ChangePath("Path");
        await Assert.That((uri with { Path = uri.Path with { Fragment = "anchor" }}).ToString()).IsEqualTo("http://www.example.org/test/uri/Path#anchor");
    }

    [Test]
    public async Task ChangeMultipleRelativePaths(){
        Uri uri = "http://example.org/test/uri";
        await Assert.That(uri.ChangePath("sub1/sub2").ToString()).IsEqualTo("http://example.org/test/uri/sub1/sub2");
    }

    [Test]
    public async Task ChangeAbsolutePath(){
        Uri uri = SimpleUri;
        await Assert.That(uri.ChangePath("test/")
                            .ChangePath("/absolute/path").ToString()).IsEqualTo("http://www.example.org/absolute/path");
    }

    [Test]
    public async Task ChangePathWithInvalidCharactersMustThrow(){
        Uri uri = SimpleUri;
        await Assert.That(() => uri.ChangePath("path?a=b&123#fragment!")).Throws<ArgumentException>();
    }

    #endregion

    [Test]
    public async Task QueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456&note&a b=999&this%20key=value%20with%20spaces";
        await Assert.That(uri.ClearQuery().Query("a")).IsNull();
        await Assert.That(uri.QueryToString("a")).IsEqualTo("123");
        await Assert.That(uri.QueryToString("b")).IsEqualTo("456");
        await Assert.That(uri.QueryToString("a b")).IsEqualTo("999");
        await Assert.That(uri.QueryToString("this key")).IsEqualTo("value with spaces");
        await Assert.That(uri.QueryToString("note")).IsEqualTo(string.Empty);
        await Assert.That(uri.Query("invalid")).IsNull();
    }

    [Test]
    public async Task AddQueryWithInvalidCharacters() {
        Uri uri = "http://example.org/params?my discount=20%25";
        var newUri = uri.UpdateQuery("your discount", "30%").UpdateQuery("formula", "x = y%25");

        await Assert.That(newUri.QueryToString("my discount")).IsEqualTo("20%");
        await Assert.That(newUri.QueryToString("your discount")).IsEqualTo("30%");
        await Assert.That(newUri.QueryToString("formula")).IsEqualTo("x = y%25");
    }

    [Test]
    public async Task ReplaceQueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456";

        var newUri = uri.UpdateQuery("c", "999").ReplaceQuery("a", "000").ReplaceQuery("b");

        await Assert.That(newUri.QueryToString("a")).IsEqualTo("000");
        await Assert.That(newUri.QueryToString("b")).IsEqualTo(string.Empty);
        await Assert.That(newUri.QueryToString("c")).IsEqualTo("999");

        var parts = newUri.ToString().Split('?');
        await Assert.That(parts[0]).IsEqualTo("http://example.org/params");

        var queries = parts[1].Split('&');
        await Assert.That(queries).IsEquivalentTo(new[] { "a=000", "b", "c=999" });
        await Assert.That(newUri).IsEqualTo((Uri)"http://example.org/params?b&a=000&c=999");
    }

    [Test]
    public async Task MultipleQueryStringParse() {
        Uri uri = "http://example.org/params?a=123&a=456";

        await Assert.That(uri.Query("a")!.Value.ToArray()).IsEquivalentTo(new string?[] { "123", "456" });
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/params?a=123&a=456");
    }

    [Test]
    public async Task SameMultipleQueryStringValueAreNotDuplicated() {
        Uri uri = "http://example.org/params?a=123&a=123";

        await Assert.That(uri.QueryToString("a")).IsEqualTo("123");
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/params?a=123");
    }

    [Test]
    public async Task SetCredentials(){
        Uri uri = SimpleUri;
        var newUri = uri.SetCredentials("admin", "fake");

        await Assert.That(newUri.ToString()).IsEqualTo("http://admin:fake@www.example.org/");
        // ReSharper disable once RedundantArgumentDefaultValue
        await Assert.That(() => uri.SetCredentials("admin", null)).Throws<ArgumentException>();
        await Assert.That(newUri.SetCredentials()).IsEqualTo(uri);
        await Assert.That(newUri.SetCredentials(password: null)).IsEqualTo(newUri.SetCredentials());
    }

    [Test]
    public async Task SetCredentialsProperlyEncodedWhenSerializedToString(){
        Uri uri = SimpleUri;
        await Assert.That(uri.SetCredentials("admin", "space in password")
                            .ToString())
                    .IsEqualTo("http://admin:space%20in%20password@www.example.org/");
    }

    [Test]
    public async Task GetFragment(){
        Uri uri = "http://example.org#whatever%20it%20is";

        await Assert.That(uri.Path.Fragment).IsEqualTo("whatever it is");
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/#whatever%20it%20is");
    }

    [Test]
    public async Task SetFragment(){
        Uri uri = "http://example.org/params?a=000&c=999";
        await Assert.That(uri.SetFragment("hello").ToString()).IsEqualTo("http://example.org/params?a=000&c=999#hello");
        await Assert.That(uri.SetFragment("hello").SetFragment().ToString()).IsEqualTo("http://example.org/params?a=000&c=999");
    }

    [Test]
    public async Task UseHttpBuilder(){
        var uri = Uri.Http
                     .Host("example.org")
                     .ChangePath("test/uri")
                     .SetPort(8000)
                     .UpdateQuery(("a", "123"), ("b", "456"))
                     .SetFragment("fragment")
                     .SetCredentials("user", "password");
        await Assert.That(uri.ToString()).IsEqualTo("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");
    }

    [Test]
    public async Task CustomScheme(){
        var uri = Uri.From("akka://my-sys/user");
        await Assert.That(uri.Path.Paths).IsEquivalentTo(new[] { "", "user" });
        await Assert.That(uri.ToString()).IsEqualTo("akka://my-sys/user");
    }

    [Test]
    public async Task TestFileUri(){
        var uri = Uri.File.Host().ChangePath("c:/WINDOWS/system.ini");
        await Assert.That(uri.ToString()).IsEqualTo("file:///c%3A/WINDOWS/system.ini");
    }
}
