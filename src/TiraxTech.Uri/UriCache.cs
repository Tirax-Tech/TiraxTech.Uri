using System;

// ReSharper disable MemberCanBePrivate.Global
namespace TiraxTech;

public readonly struct UriCache : IEquatable<UriCache>
{
    public Uri Uri{ get; }
    public System.Uri SystemUri{ get; }

    UriCache(Uri uri, System.Uri systemUri) {
        Uri = uri;
        SystemUri = systemUri;
    }

    // From(Uri) is fallible because ToSystemUri() is — a cache always holds a valid System.Uri.
    public static Outcome<UriCache> From(string uri) => Uri.From(uri).Bind(From);
    public static Outcome<UriCache> From(Uri uri) => uri.ToSystemUri().Map(systemUri => new UriCache(uri, systemUri));

    public override string ToString() => SystemUri.ToString();

    // Equality is aligned across all members on SystemUri: only UriCache and System.Uri are comparable
    // (defect #18). String/Uri-record cross-type comparison is intentionally not supported.
    public override bool Equals(object? obj) =>
        obj switch
        { UriCache cache => cache.SystemUri == SystemUri,
          System.Uri uri => uri == SystemUri,
          _              => false };

    public override int GetHashCode() => SystemUri.GetHashCode();

    public bool Equals(UriCache other) => SystemUri.Equals(other.SystemUri);

    public static bool operator ==(UriCache left, UriCache right) => left.Equals(right);
    public static bool operator !=(UriCache left, UriCache right) => !left.Equals(right);
}

public static class UriCacheExtension
{
    public static Outcome<UriCache> Cached(this Uri uri) => UriCache.From(uri);
}
