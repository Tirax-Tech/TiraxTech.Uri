// ReSharper disable MemberCanBePrivate.Global
namespace TiraxTech;

public readonly struct UriCache(Uri uri)
{
    public Uri Uri{ get; } = uri;
    public System.Uri SystemUri{ get; } = uri.ToSystemUri();

    public static UriCache From(string uri) => new(uri);
    public static UriCache From(Uri uri) => new(uri);
    public override string ToString() => SystemUri.ToString();

    public override bool Equals(object? obj) =>
        obj switch
        { UriCache cache => cache.SystemUri == SystemUri,
          System.Uri uri => uri == SystemUri,
          Uri uri        => Uri == uri,
          string s       => Uri == s,
          _              => false };

    public override int GetHashCode() => SystemUri.GetHashCode();
}

public static class UriCacheExtension
{
    public static UriCache Cached(this Uri uri) => UriCache.From(uri);
}