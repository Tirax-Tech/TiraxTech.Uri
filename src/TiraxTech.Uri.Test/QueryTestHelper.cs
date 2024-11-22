using FluentAssertions;

namespace TiraxTech.UriTest;

public static class QueryTestHelper
{
    public static string QueryToString(this Uri uri, string key) {
        var result = uri.Query(key);
        result.Should().NotBeNull();
        return result!.Value.ToString();
    }
}