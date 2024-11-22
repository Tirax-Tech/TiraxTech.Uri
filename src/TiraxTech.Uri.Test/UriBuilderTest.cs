using System;
using FluentAssertions;
using Xunit;

namespace TiraxTech.UriTest;

public class UriBuilderTest
{
    const string SimpleUri = "http://www.example.org";
    const string SimpleUriFormatted = "http://www.example.org/";

    [Theory]
    [InlineData(SimpleUri, SimpleUriFormatted)]
    [InlineData("https://example.org", "https://example.org/")]
    [InlineData("ftp://example.org/path", "ftp://example.org/path")]
    [InlineData("ws://example.org/service", "ws://example.org/service")]
    [InlineData("ldap://example.org/service", "ldap://example.org/service")]
    [InlineData("net.tcp://example.org/service", "net.tcp://example.org/service")]
    [InlineData("net.pipe://example.org/service", "net.pipe://example.org/service")]
    public void ConvertBetweenStringAndUri(string originUri, string formattedUri)
    {
        Uri uri = originUri;
        var uri1 = Uri.From(originUri);

        uri.Should().BeEquivalentTo(uri1);
        uri.ToString().Should().Be(formattedUri);
    }

    [Fact]
    public void ChangePortAndFragmentAltogether(){
        Uri uri = SimpleUri;

        (uri with { Port = 123, Path = uri.Path with { Fragment = "test"}}).ToString().Should().Be("http://www.example.org:123/#test");
        uri.ToString().Should().Be(SimpleUriFormatted);
    }

    #region Path tests

    [Fact]
    public void ChangeRelativePath(){
        Uri uri = SimpleUri;
        uri = uri.ChangePath("test");
        (uri with { Path = uri.Path with { Fragment = "anchor"}}).ToString().Should().Be("http://www.example.org/test#anchor");
    }

    [Fact]
    public void ChainChangeRelativePath(){
        Uri uri = SimpleUri;
        uri = uri.ChangePath("test").ChangePath("uri").ChangePath("Path");
        (uri with { Path = uri.Path with { Fragment = "anchor" }}).ToString().Should().Be("http://www.example.org/test/uri/Path#anchor");
    }

    [Fact]
    public void ChangeMultipleRelativePaths(){
        Uri uri = "http://example.org/test/uri";
        uri.ChangePath("sub1/sub2").ToString().Should().Be("http://example.org/test/uri/sub1/sub2");
    }

    [Fact]
    public void ChangeAbsolutePath(){
        Uri uri = SimpleUri;
        uri.ChangePath("test/")
           .ChangePath("/absolute/path").ToString().Should().Be("http://www.example.org/absolute/path");
    }

    [Fact]
    public void ChangePathWithInvalidCharactersMustThrow(){
        Uri uri = SimpleUri;
        Action test = () => uri.ChangePath("path?a=b&123#fragment!");
        test.Should().Throw<ArgumentException>();
    }

    #endregion

    [Fact]
    public void QueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456&note&a b=999&this%20key=value%20with%20spaces";
        uri.ClearQuery().Query("a").Should().BeNull();
        uri.QueryToString("a").Should().Be("123");
        uri.QueryToString("b").Should().Be("456");
        uri.QueryToString("a b").Should().Be("999");
        uri.QueryToString("this key").Should().Be("value with spaces");
        uri.QueryToString("note").Should().Be(string.Empty);
        uri.Query("invalid").Should().BeNull();
    }

    [Fact]
    public void AddQueryWithInvalidCharacters() {
        Uri uri = "http://example.org/params?my discount=20%25";
        var newUri = uri.UpdateQuery("your discount", "30%").UpdateQuery("formula", "x = y%25");

        newUri.QueryToString("my discount").Should().Be("20%");
        newUri.QueryToString("your discount").Should().Be("30%");
        newUri.QueryToString("formula").Should().Be("x = y%25");
    }

    [Fact]
    public void ReplaceQueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456";

        var newUri = uri.UpdateQuery("c", "999").ReplaceQuery("a", "000").ReplaceQuery("b");

        newUri.QueryToString("a").Should().Be("000");
        newUri.QueryToString("b").Should().BeEmpty();
        newUri.QueryToString("c").Should().Be("999");

        var parts = newUri.ToString().Split('?');
        parts[0].Should().Be("http://example.org/params");

        var queries = parts[1].Split('&');
        queries.Should().BeEquivalentTo("a=000", "b", "c=999");
        newUri.Should().Be((Uri)"http://example.org/params?b&a=000&c=999");
    }

    [Fact]
    public void MultipleQueryStringParse() {
        Uri uri = "http://example.org/params?a=123&a=456";

        uri.Query("a")!.Value.ToArray().Should().BeEquivalentTo("123", "456");
        uri.ToString().Should().BeOneOf("http://example.org/params?a=456&a=123", "http://example.org/params?a=123&a=456");
    }

    [Fact]
    public void SameMultipleQueryStringValueAreNotDuplicated() {
        Uri uri = "http://example.org/params?a=123&a=123";

        uri.QueryToString("a").Should().Be("123");
        uri.ToString().Should().Be("http://example.org/params?a=123");
    }

    [Fact]
    public void SetCredentials(){
        Uri uri = SimpleUri;
        var newUri = uri.SetCredentials("admin", "fake");

        newUri.ToString().Should().Be("http://admin:fake@www.example.org/");
        // ReSharper disable once RedundantArgumentDefaultValue
        new Action(() => uri.SetCredentials("admin", null)).Should().Throw<ArgumentException>();
        newUri.SetCredentials().Should().Be(uri);
        newUri.SetCredentials(password: null).Should().Be(newUri.SetCredentials());
    }

    [Fact]
    public void SetCredentialsProperlyEncodedWhenSerializedToString(){
        Uri uri = SimpleUri;
        uri.SetCredentials("admin", "space in password")
           .ToString()
           .Should().Be("http://admin:space%20in%20password@www.example.org/");
    }

    [Fact]
    public void GetFragment(){
        Uri uri = "http://example.org#whatever%20it%20is";

        uri.Path.Fragment.Should().Be("whatever it is");
        uri.ToString().Should().Be("http://example.org/#whatever%20it%20is");
    }

    [Fact]
    public void SetFragment(){
        Uri uri = "http://example.org/params?a=000&c=999";
        uri.SetFragment("hello").ToString().Should().Be("http://example.org/params?a=000&c=999#hello");
        uri.SetFragment("hello").SetFragment().ToString().Should().Be("http://example.org/params?a=000&c=999");
    }

    [Fact]
    public void UseHttpBuilder(){
        var uri = Uri.Http
                     .Host("example.org")
                     .ChangePath("test/uri")
                     .SetPort(8000)
                     .UpdateQuery(("a", "123"), ("b", "456"))
                     .SetFragment("fragment")
                     .SetCredentials("user", "password");
        uri.ToString().Should().Be("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");
    }

    [Fact]
    public void CustomScheme(){
        var uri = Uri.From("akka://my-sys/user");
        uri.Path.Paths.Length.Should().Be(1);
        uri.Path.Paths[0].Should().Be("user");
        uri.ToString().Should().Be("akka://my-sys/user");
    }

    [Fact]
    public void TestFileUri(){
        var uri = Uri.File.Host().ChangePath("c:/WINDOWS/system.ini");
        uri.ToString().Should().Be("file:///c%3A/WINDOWS/system.ini");
    }
}