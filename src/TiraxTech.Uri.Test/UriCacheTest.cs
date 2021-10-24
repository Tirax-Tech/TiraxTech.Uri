using FluentAssertions;
using Xunit;

namespace TiraxTech.UriTest;

public sealed class UriCacheTest
{
    [Fact]
    public void EqualityTests(){
        const string testUri = "http://example.org/test";
        var uri = UriCache.From(testUri);
        var uri2 = UriCache.From(testUri);
        var uri3 = Uri.From(testUri);

        uri.Should().Be(uri2);
        uri.Should().Be(new System.Uri(testUri));
        uri.Should().Be(testUri);
        uri.Should().Be(uri3.Cache());

        uri.SystemUri.Should().Be(new System.Uri(testUri));
        uri.Uri.Should().Be(uri2.Uri);
    }
}