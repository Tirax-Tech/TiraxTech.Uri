using FluentAssertions;
using RZ.Foundation.Extensions;

namespace TiraxTech.UriTest;

public static class QueryTestHelper
{
    public static string QueryToString(this Uri uri, string key) {
        var result = uri.Query(key);
        result.IsSome.Should().BeTrue();
        return result.Get().ToString();
    }
}