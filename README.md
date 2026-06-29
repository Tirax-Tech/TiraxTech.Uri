# Tirax Tech's URI Builder #

An immutable, reusable URI builder/parser. Parsing and other fallible operations return
[`RZ.Foundation`](https://www.nuget.org/packages/RZ.Foundation)'s `Outcome<T>` instead of throwing.

> **Breaking change:** operations that can fail — `Uri.From`, the scheme builders' `.Host(...)`,
> `ChangePath`, `SetCredentials`/`ValidateCredentials`, and `UriCache.From(string)` — now return
> `Outcome<…>`, and the implicit `string → Uri` conversion has been removed. Compose fallible steps
> with LINQ (`from … select`) or `Bind`; total operations (`SetPort`, `SetFragment`, and the query
> methods) still return `Uri` directly. `ToString()`/`ToSystemUri()` never throw.

## Quick Example ##

To build this URI:

```
http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Write:

```c#
using TiraxTech;
using RZ.Foundation;   // brings in LINQ support for Outcome<T>

var result =
    from u        in Uri.Http.Host("example.org")
    from withPath in u.ChangePath("test/uri")
    from full     in withPath.SetPort(8000)
                             .UpdateQuery(("a", "123"), ("b", "456"))
                             .SetFragment("fragment")
                             .SetCredentials("user", "password")
    select full;     // result is Outcome<Uri>

// Handle the result. Unwrap() throws on failure; prefer Match/IfFail/IsSuccess for untrusted input.
Console.WriteLine(result.Unwrap());
// http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Handle failures explicitly rather than unwrapping:

```c#
var text = Uri.From(userInput)
              .Match(uri => uri.ToString(),
                     err => $"bad uri ({err.Code}): {err.Message}");
```

Adding, removing, and replacing query parameters are total operations (they return `Uri`):

```c#
var uri  = result.Unwrap();
var uri2 = uri.UpdateQuery("c", "789")     // append
              .RemoveQuery("a")
              .ReplaceQuery("c", "123")     // replace
              .UpdateQuery("b", "789");     // multi-value query string!

Console.WriteLine(uri2);
// http://user:password@example.org:8000/test/uri?b=456&b=789&c=123#fragment
```

## What's different from other URI builder libs? ##

`TiraxTech.Uri` is designed to be immutable and reusable. Because it is immutable, a URI value can be
reused as a base for many derived URIs without side effects.

```c#
var baseApi = Uri.Https.Host("example.org")
                       .Bind(u => u.ChangePath("api/search"))
                       .Map(u => u.UpdateQuery("q", "beer"))
                       .Unwrap();

var searchWine    = baseApi.ReplaceQuery("q", "wine");
var searchSpecial = searchWine.ChangePath("special").Unwrap();

Console.WriteLine(baseApi);         // https://example.org/api/search?q=beer
Console.WriteLine(searchWine);      // https://example.org/api/search?q=wine
Console.WriteLine(searchSpecial);   // https://example.org/api/search/special?q=wine
```

> **Note on untrusted input:** the parser accepts arbitrary/custom schemes (e.g. `akka://`,
> `net.tcp://`) by design and does **not** reject dangerous schemes (e.g. `javascript:`) or
> dot-segments (`..`). `ToSystemUri()` applies RFC 3986 dot-segment normalisation, so a `..`
> appended via `ChangePath` can climb above the intended base path. Vet untrusted scheme/path
> input yourself.

## RelativeUri ##

Represents a relative URI. It helps when building a full URI from a base URI.

## UriCache ##

`TiraxTech.Uri` composes the URI string every time `ToString()` or `ToSystemUri()` is called. Use the
`Cached()` extension to cache the composed URI:

```c#
using TiraxTech;

var cached = Uri.Https.Host("google.com").Map(u => u.Cached()).Unwrap();

Console.WriteLine(cached.ToString());  // uses the cached value
```
