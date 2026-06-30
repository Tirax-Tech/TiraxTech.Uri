# Tirax Tech's URI Builder #

An immutable, reusable URI builder/parser. Parsing and other fallible operations return
[`RZ.Foundation`](https://www.nuget.org/packages/RZ.Foundation)'s `Outcome<T>` instead of throwing.

> **Breaking change:** operations that can fail — `Uri.From`, the scheme builders' `.Host(...)`,
> `ChangePath`, `SetCredentials`/`ValidateCredentials`, `UriCache.From`, and `ToSystemUri()` — return
> `Outcome<…>`, and the implicit `string → Uri` conversion has been removed. The builder operations
> are also lifted onto `Outcome<Uri>`, so you can keep chaining directly and a failure short-circuits
> the rest of the chain. `ToString()` is a faithful serializer (never throws); `ToSystemUri()` returns
> `Outcome<System.Uri>`.

## Quick Example ##

To build this URI:

```
http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Write:

```c#
using TiraxTech;

var result = Uri.Http.Host("example.org")
                .ChangePath("test/uri")
                .SetPort(8000)
                .UpdateQuery(("a", "123"), ("b", "456"))
                .SetFragment("fragment")
                .SetCredentials("user", "password");   // result is Outcome<Uri>

// Unwrap() throws on failure; prefer Match/IfFail/IsSuccess for untrusted input.
Console.WriteLine(result.Unwrap());
// http://user:password@example.org:8000/test/uri?a=123&b=456#fragment
```

Handle failures explicitly rather than unwrapping:

```c#
var text = Uri.From(userInput)
              .Match(uri => uri.ToString(),
                     err => $"bad uri ({err.Code}): {err.Message}");
```

The chain is sugar over `Outcome<T>`'s combinators (`Map`/`Bind`). When you need to interleave other
`Outcome` steps, use the Go-style guard pattern (preferred over LINQ for `Outcome<T>`):

```c#
using RZ.Foundation;
using static RZ.Foundation.AOT.Prelude;   // Fail / Success guards

Outcome<Uri> BuildEndpoint(string host) {
    if (Fail(Uri.Http.Host(host).ChangePath("api/v1"), out var e, out var uri))
        return e.Trace();
    return uri.SetFragment("top");
}
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
                       .ChangePath("api/search")
                       .UpdateQuery("q", "beer")
                       .Unwrap();

var searchWine    = baseApi.ReplaceQuery("q", "wine");
var searchSpecial = searchWine.ChangePath("special").Unwrap();

Console.WriteLine(baseApi);         // https://example.org/api/search?q=beer
Console.WriteLine(searchWine);      // https://example.org/api/search?q=wine
Console.WriteLine(searchSpecial);   // https://example.org/api/search/special?q=wine
```

> **Note on construction & untrusted input:** build URIs via `Uri.From` or the scheme builders —
> they validate their input. Mutating `Scheme`/`Host` through a raw `with` expression bypasses that
> validation and is unsupported: `ToString()` is a faithful serializer that still renders the value
> as-is, but `ToSystemUri()` (a fallible `Outcome<System.Uri>` conversion) will then fail. The parser
> accepts arbitrary/custom schemes (e.g. `akka://`, `net.tcp://`) by design and does **not** reject
> dangerous schemes (e.g. `javascript:`) or dot-segments (`..`); a `..` appended via `ChangePath` is
> normalised by `ToSystemUri()` (RFC 3986) and can climb above the intended base path. Vet untrusted
> scheme/host/path input yourself.

## RelativeUri ##

Represents a relative URI. It helps when building a full URI from a base URI.

## UriCache ##

`TiraxTech.Uri` composes the URI string every time `ToString()` or `ToSystemUri()` is called. Use the
`Cached()` extension to cache the composed URI:

```c#
using TiraxTech;

var cached = Uri.Https.Host("google.com").Cached().Unwrap();

Console.WriteLine(cached.ToString());  // uses the cached value
```
