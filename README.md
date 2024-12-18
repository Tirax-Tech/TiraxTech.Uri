# Tirax Tech's URI Builder #

## Quick Example ##

To build this URI:

```
http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Write:

```c#
using TiraxTech;

var uri = Uri.Http
             .Host("example.org")
             .ChangePath("test/uri")
             .SetPort(8000)
             .UpdateQuery(("a", "123"), ("b", "456"))
             .SetFragment("fragment")
             .SetCredentials("user", "password");

Console.WriteLine(uri); // http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Adding, removing, and replacing query parameters is possible too.

```c#
var uri2 = uri.UpdateQuery("c", "789")
              .RemoveQuery("a")
              .ReplaceQuery("c", "123")
              .UpdateQuery("b", "789")  // multi-value query string!
              .SetFragment("fragment")
              .SetCredentials("user", "password");

Console.WriteLine(uri2); // http://user:password@example.org:8000/test/uri?c=123&b=456&b=789#fragment
```

## What's different from other URI builder libs? ##

`TiraxTech.Uri` is designed with immutable and reusable in mind. Since it is immutable, an URI
can be reused multiple times.

```c#
var baseApi = Uri.Https.Host("example.org")
                       .ChangePath("api/search")
                       .UpdateQuery("q", "beer");
                      
var searchWine = baseApi.UpdateQuery("q", "wine");
var searchSpecial = searchWine.ChangePath("special");
var searchMerlot = searchWine.UpdateQuery("type", "merlot")
                             .ChangePath("/api/v2/search");

Console.WriteLine(baseApi);         // https://example.org:443/api/search?q=beer
Console.WriteLine(searchWine);      // https://example.org:443/api/search?q=wine
Console.WriteLine(searchSpecial);   // https://example.org:443/api/search/special?q=wine
Console.WriteLine(searchMerlot);    // https://example.org:443/api/v2/search?q=wine&type=merlot
```

## RelativeUri ##

Represent a relative URI. It helps when building a full URI from a base URI.

## UriCache ##

Since `TiraxTech.Uri` composes URI string everytime the method `ToString()` or `ToSystemUri()`
is called.  You may want to use `Cached()` extension method to cache the composed URI.

```c#
using TiraxTech;

var cached = Uri.Https.Host("google.com").Cached();

Console.WriteLine(cached.ToString());  // use cached version
```