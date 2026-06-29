namespace TiraxTech.UriTest;

public static class QueryTestHelper
{
    public static string QueryToString(this Uri uri, string key) {
        var result = uri.Query(key);
        return result is null
            ? throw new InvalidOperationException($"Query parameter '{key}' was expected to exist but was null.")
            : result.Value.ToString();
    }
}
