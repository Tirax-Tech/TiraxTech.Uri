using System;

// ReSharper disable MemberCanBePrivate.Global
namespace TiraxTech;

public readonly struct UriCache(Uri uri) : IEquatable<UriCache>
{
    public Uri Uri{ get; } = uri;
    public System.Uri SystemUri{ get; } = uri.ToSystemUri();

    public static Outcome<UriCache> From(string uri) => Uri.From(uri).Map(u => new UriCache(u));
    public static UriCache From(Uri uri) => new(uri);
    public override string ToString() => SystemUri.ToString();

    // Equality is aligned with GetHashCode (which uses SystemUri): only UriCache and System.Uri are
    // comparable (defect #18). String/Uri-record cross-type comparison is intentionally not supported.
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
    public static UriCache Cached(this Uri uri) => UriCache.From(uri);
}
