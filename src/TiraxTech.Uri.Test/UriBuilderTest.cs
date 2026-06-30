using RZ.Foundation;

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
        var uri = Uri.From(originUri).Unwrap();
        var uri1 = Uri.From(originUri).Unwrap();

        await Assert.That(uri).IsEqualTo(uri1);
        await Assert.That(uri.ToString()).IsEqualTo(formattedUri);
    }

    [Test]
    public async Task ChangePortAndFragmentAltogether(){
        var uri = Uri.From(SimpleUri).Unwrap();

        await Assert.That((uri with { Port = 123, Path = uri.Path with { Fragment = "test"}}).ToString()).IsEqualTo("http://www.example.org:123/#test");
        await Assert.That(uri.ToString()).IsEqualTo(SimpleUriFormatted);
    }

    #region Path tests

    [Test]
    public async Task ChangeRelativePath(){
        var uri = Uri.From(SimpleUri).Unwrap().ChangePath("test").Unwrap();
        await Assert.That((uri with { Path = uri.Path with { Fragment = "anchor"}}).ToString()).IsEqualTo("http://www.example.org/test#anchor");
    }

    [Test]
    public async Task ChainChangeRelativePath(){
        var uri = Uri.From(SimpleUri).Unwrap()
                     .ChangePath("test").Bind(u => u.ChangePath("uri")).Bind(u => u.ChangePath("Path")).Unwrap();
        await Assert.That((uri with { Path = uri.Path with { Fragment = "anchor" }}).ToString()).IsEqualTo("http://www.example.org/test/uri/Path#anchor");
    }

    [Test]
    public async Task ChangeMultipleRelativePaths(){
        var uri = Uri.From("http://example.org/test/uri").Unwrap();
        await Assert.That(uri.ChangePath("sub1/sub2").Unwrap().ToString()).IsEqualTo("http://example.org/test/uri/sub1/sub2");
    }

    [Test]
    public async Task ChangeAbsolutePath(){
        var uri = Uri.From(SimpleUri).Unwrap()
                     .ChangePath("test/").Bind(u => u.ChangePath("/absolute/path")).Unwrap();
        await Assert.That(uri.ToString()).IsEqualTo("http://www.example.org/absolute/path");
    }

    [Test]
    [DisplayName("ChangePath with invalid characters fails with a path.invalid-char error")]
    public async Task ChangePathWithInvalidCharactersFails(){
        var uri = Uri.From(SimpleUri).Unwrap();
        var result = uri.ChangePath("path?a=b&123#fragment!");
        await Assert.That(result.IsFail).IsTrue();
        await Assert.That(result.UnwrapError().Code).IsEqualTo(UriError.InvalidPathChar);
    }

    #endregion

    [Test]
    public async Task QueryParamItem(){
        var uri = Uri.From("http://example.org/params?a=123&b=456&note&a b=999&this%20key=value%20with%20spaces").Unwrap();
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
        var uri = Uri.From("http://example.org/params?my discount=20%25").Unwrap();
        var newUri = uri.UpdateQuery("your discount", "30%").UpdateQuery("formula", "x = y%25");

        await Assert.That(newUri.QueryToString("my discount")).IsEqualTo("20%");
        await Assert.That(newUri.QueryToString("your discount")).IsEqualTo("30%");
        await Assert.That(newUri.QueryToString("formula")).IsEqualTo("x = y%25");
    }

    [Test]
    public async Task ReplaceQueryParamItem(){
        var uri = Uri.From("http://example.org/params?a=123&b=456").Unwrap();

        var newUri = uri.UpdateQuery("c", "999").ReplaceQuery("a", "000").ReplaceQuery("b");

        await Assert.That(newUri.QueryToString("a")).IsEqualTo("000");
        await Assert.That(newUri.QueryToString("b")).IsEqualTo(string.Empty);
        await Assert.That(newUri.QueryToString("c")).IsEqualTo("999");

        var parts = newUri.ToString().Split('?');
        await Assert.That(parts[0]).IsEqualTo("http://example.org/params");

        var queries = parts[1].Split('&');
        await Assert.That(queries).IsEquivalentTo(new[] { "a=000", "b", "c=999" });
        await Assert.That(newUri).IsEqualTo(Uri.From("http://example.org/params?b&a=000&c=999").Unwrap());
    }

    [Test]
    public async Task MultipleQueryStringParse() {
        var uri = Uri.From("http://example.org/params?a=123&a=456").Unwrap();

        await Assert.That(uri.Query("a")!.Value.ToArray()).IsEquivalentTo(new string?[] { "123", "456" });
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/params?a=123&a=456");
    }

    [Test]
    public async Task SameMultipleQueryStringValueAreNotDuplicated() {
        var uri = Uri.From("http://example.org/params?a=123&a=123").Unwrap();

        await Assert.That(uri.QueryToString("a")).IsEqualTo("123");
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/params?a=123");
    }

    [Test]
    public async Task SetCredentials(){
        var uri = Uri.From(SimpleUri).Unwrap();
        var newUri = uri.SetCredentials("admin", "fake").Unwrap();

        await Assert.That(newUri.ToString()).IsEqualTo("http://admin:fake@www.example.org/");

        var missingPassword = uri.SetCredentials("admin", null);
        await Assert.That(missingPassword.IsFail).IsTrue();
        await Assert.That(missingPassword.UnwrapError().Code).IsEqualTo(UriError.PasswordRequired);

        await Assert.That(newUri.SetCredentials().Unwrap()).IsEqualTo(uri);
        await Assert.That(newUri.SetCredentials(password: null).Unwrap()).IsEqualTo(newUri.SetCredentials().Unwrap());
    }

    [Test]
    public async Task SetCredentialsProperlyEncodedWhenSerializedToString(){
        var uri = Uri.From(SimpleUri).Unwrap();
        await Assert.That(uri.SetCredentials("admin", "space in password").Unwrap()
                            .ToString())
                    .IsEqualTo("http://admin:space%20in%20password@www.example.org/");
    }

    [Test]
    public async Task GetFragment(){
        var uri = Uri.From("http://example.org#whatever%20it%20is").Unwrap();

        await Assert.That(uri.Path.Fragment).IsEqualTo("whatever it is");
        await Assert.That(uri.ToString()).IsEqualTo("http://example.org/#whatever%20it%20is");
    }

    [Test]
    public async Task SetFragment(){
        var uri = Uri.From("http://example.org/params?a=000&c=999").Unwrap();
        await Assert.That(uri.SetFragment("hello").ToString()).IsEqualTo("http://example.org/params?a=000&c=999#hello");
        await Assert.That(uri.SetFragment("hello").SetFragment().ToString()).IsEqualTo("http://example.org/params?a=000&c=999");
    }

    [Test]
    public async Task UseHttpBuilder(){
        var uri = Uri.Http.Host("example.org")
                     .ChangePath("test/uri")
                     .SetPort(8000)
                     .UpdateQuery(("a", "123"), ("b", "456"))
                     .SetFragment("fragment")
                     .SetCredentials("user", "password")
                     .Unwrap();
        await Assert.That(uri.ToString()).IsEqualTo("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");
    }

    [Test]
    public async Task CustomScheme(){
        var uri = Uri.From("akka://my-sys/user").Unwrap();
        await Assert.That(uri.Path.Paths).IsEquivalentTo(new[] { "", "user" });
        await Assert.That(uri.ToString()).IsEqualTo("akka://my-sys/user");
    }

    [Test]
    public async Task TestFileUri(){
        var uri = Uri.File.Host().Bind(u => u.ChangePath("c:/WINDOWS/system.ini")).Unwrap();
        await Assert.That(uri.ToString()).IsEqualTo("file:///c%3A/WINDOWS/system.ini");
    }
}
