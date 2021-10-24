using System;
using FluentAssertions;
using Xunit;

namespace TiraxTech.UriTest;

public class UriBuilderTest
{
    const string SimpleUri = "http://www.example.org";
    const string SimpleUriFormatted = "http://www.example.org:80/";
    
    [Fact]
    public void ConvertBetweenStringAndUri()
    {
        Uri uri = SimpleUri;
        var uri1 = Uri.From(SimpleUri);

        uri.Should().BeEquivalentTo(uri1);
        uri.ToString().Should().Be(SimpleUriFormatted);
    }

    [Fact]
    public void ChangePortAndFragmentAltogether(){
        Uri uri = SimpleUri;

        (uri with { Port = 123, Fragment = "test" }).ToString().Should().Be("http://www.example.org:123/#test");
        uri.ToString().Should().Be(SimpleUriFormatted);
    }

    [Fact]
    public void ChangeRelativePath(){
        Uri uri = SimpleUri;
        (uri.ChangePath("test") with { Fragment = "anchor"}).ToString().Should().Be("http://www.example.org:80/test#anchor");
    }

    [Fact]
    public void ChainChangeRelativePath(){
        Uri uri = SimpleUri;
        (uri.ChangePath("test")
            .ChangePath("uri")
            .ChangePath("Path") with
         { Fragment = "anchor" }
            ).ToString().Should()
             .Be("http://www.example.org:80/test/uri/Path#anchor");
    }

    [Fact]
    public void ChangeMultipleRelativePaths(){
        Uri uri = "http://example.org/test/uri";
        uri.ChangePath("sub1/sub2").ToString().Should().Be("http://example.org:80/test/uri/sub1/sub2");
    }

    [Fact]
    public void ChangeAbsolutePath(){
        Uri uri = SimpleUri;
        uri.ChangePath("test/")
           .ChangePath("/absolute/path").ToString().Should().Be("http://www.example.org:80/absolute/path");
    }

    [Fact]
    public void ChangePathWithInvalidCharactersMustThrow(){
        Uri uri = SimpleUri;
        Action test = () => uri.ChangePath("path?a=b&123#fragment!");
        test.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void QueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456&note&a b=999&this%20key=value%20with%20spaces";
        uri.ClearQuery().Query("a").Should().BeNull();
        uri.Query("a").Should().Be("123");
        uri.Query("b").Should().Be("456");
        uri.Query("a b").Should().Be("999");
        uri.Query("this key").Should().Be("value with spaces");
        uri.Query("note").Should().Be(string.Empty);
        uri.Query("invalid").Should().BeNull();
    }

    [Fact]
    public void SetQueryParamItem(){
        Uri uri = "http://example.org/params?a=123&b=456";

        var newUri = uri.SetQuery("c", "999").SetQuery("a", "000").SetQuery("b");

        newUri.Query("a").Should().Be("000");
        newUri.Query("b").Should().BeNull();
        newUri.Query("c").Should().Be("999");
        newUri.Should().Be((Uri)"http://example.org/params?a=000&c=999");
        newUri.ToString().Should().Be("http://example.org:80/params?a=000&c=999");
    }

    [Fact]
    public void SetCredentials(){
        Uri uri = SimpleUri;
        var newUri = uri.SetCredentials("admin", "fake");
        
        newUri.ToString().Should().Be("http://admin:fake@www.example.org:80/");
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
           .Should().Be("http://admin:space%20in%20password@www.example.org:80/");
    }

    [Fact]
    public void GetFragment(){
        Uri uri = "http://example.org#whatever%20it%20is";
        
        uri.Fragment.Should().Be("whatever it is");
        uri.ToString().Should().Be("http://example.org:80/#whatever%20it%20is");
    }

    [Fact]
    public void SetFragment(){
        Uri uri = "http://example.org/params?a=000&c=999";
        uri.SetFragment("hello").ToString().Should().Be("http://example.org:80/params?a=000&c=999#hello");
        uri.SetFragment("hello").SetFragment().ToString().Should().Be("http://example.org:80/params?a=000&c=999");
    }

    [Fact]
    public void UseHttpBuilder(){
        var uri = Uri.Http
                     .Host("example.org")
                     .ChangePath("test/uri")
                     .SetPort(8000)
                     .SetQuery(("a", "123"), ("b", "456"))
                     .SetFragment("fragment")
                     .SetCredentials("user", "password");
        uri.ToString().Should().Be("http://user:password@example.org:8000/test/uri?a=123&b=456#fragment");
    }

    [Fact]
    public void CustomScheme(){
        var uri = Uri.From("akka://my-sys/user");
        uri.Paths.Length.Should().Be(1);
        uri.Paths[0].Should().Be("user");
        uri.ToString().Should().Be("akka://my-sys/user");
    }

    [Fact]
    public void TestFileUri(){
        var uri = Uri.File.Host().ChangePath("c:/WINDOWS/system.ini");
        uri.ToString().Should().Be("file:///c%3A/WINDOWS/system.ini");
    }
}