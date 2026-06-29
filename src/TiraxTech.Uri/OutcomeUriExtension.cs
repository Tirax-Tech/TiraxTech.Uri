using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using RZ.Foundation;

namespace TiraxTech;

/// <summary>
/// Lifts the <see cref="Uri"/> builder operations onto <see cref="Outcome{T}"/> so a fallible chain
/// reads like the plain builder, e.g.
/// <c>Uri.Http.Host("example.org").ChangePath("a/b").SetPort(8000).SetCredentials("u","p")</c>.
/// A failure anywhere short-circuits the remaining steps.
/// </summary>
[PublicAPI]
public static class OutcomeUriExtension
{
    // Fallible operations compose with Bind.
    public static Outcome<Uri> ChangePath(this Outcome<Uri> uri, string path)
        => uri.Bind(u => u.ChangePath(path));

    public static Outcome<Uri> SetCredentials(this Outcome<Uri> uri, string? user = null, string? password = null)
        => uri.Bind(u => u.SetCredentials(user, password));

    // Total operations compose with Map.
    public static Outcome<Uri> ChangePath(this Outcome<Uri> uri, RelativeUri path)
        => uri.Map(u => u.ChangePath(path));

    public static Outcome<Uri> SetPort(this Outcome<Uri> uri, int port)
        => uri.Map(u => u.SetPort(port));

    public static Outcome<Uri> RemovePort(this Outcome<Uri> uri)
        => uri.Map(u => u.RemovePort());

    public static Outcome<Uri> SetFragment(this Outcome<Uri> uri, string? fragment = null)
        => uri.Map(u => u.SetFragment(fragment));

    public static Outcome<Uri> RemoveQuery(this Outcome<Uri> uri, string key)
        => uri.Map(u => u.RemoveQuery(key));

    public static Outcome<Uri> ReplaceQuery(this Outcome<Uri> uri, string key, StringValues? value = null)
        => uri.Map(u => u.ReplaceQuery(key, value));

    public static Outcome<Uri> UpdateQuery(this Outcome<Uri> uri, string key, StringValues? value = null)
        => uri.Map(u => u.UpdateQuery(key, value));

    public static Outcome<Uri> UpdateQuery(this Outcome<Uri> uri, params (string Key, StringValues Value)[] @params)
        => uri.Map(u => u.UpdateQuery(@params));

    public static Outcome<Uri> UpdateQueries(this Outcome<Uri> uri, IEnumerable<KeyValuePair<string, StringValues>> queries)
        => uri.Map(u => u.UpdateQueries(queries));

    public static Outcome<Uri> UpdateQueries(this Outcome<Uri> uri, IEnumerable<(string Key, StringValues Value)> queries)
        => uri.Map(u => u.UpdateQueries(queries));

    public static Outcome<Uri> ClearQuery(this Outcome<Uri> uri)
        => uri.Map(u => u.ClearQuery());

    public static Outcome<UriCache> Cached(this Outcome<Uri> uri)
        => uri.Map(u => u.Cached());
}
