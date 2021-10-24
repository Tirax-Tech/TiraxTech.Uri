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
             .SetQuery(("a", "123"), ("b", "456"))
             .SetFragment("fragment")
             .SetCredentials("user", "password");
```

## What's different from other URI builder libs? ##

`TiraxTech.Uri` is designed with immutable and reusable in mind. Since it is immutable, an URI
can be reused multiple times.

```c#
var baseApi = Uri.Https.Host("example.org")
                       .ChangePath("api/search")
                       .SetQuery("q", "beer");
                      
var searchWine = baseApi.SetQuery("q", "wine");
var searchSpecial = searchWine.ChangePath("special");
var searchMerlot = searchWine.SetQuery("type", "merlot")
                             .ChangePath("/api/v2/search");

Console.WriteLine(baseApi);         // https://example.org:443/api/search?q=beer
Console.WriteLine(searchWine);      // https://example.org:443/api/search?q=wine
Console.WriteLine(searchSpecial);   // https://example.org:443/api/search/special?q=wine
Console.WriteLine(searchMerlot);    // https://example.org:443/api/v2/search?q=wine&type=merlot
```