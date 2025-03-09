using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using QueryParamType = System.Collections.Immutable.ImmutableSortedDictionary<string, Microsoft.Extensions.Primitives.StringValues>;

namespace TiraxTech;

[PublicAPI]
public record RelativeUri(
    string[] Paths,
    QueryParamType QueryParams,
    string? Fragment
)
{
    public static implicit operator string (RelativeUri uri) => uri.ToString();

    public string PathOnly => Uri.JoinPaths(Paths);

    public static RelativeUri From(UriBuilder builder)
        => From(builder.Path, builder.Query, builder.Fragment);

    public static RelativeUri From(string path) {
        var fragmentIndex = IndexOf(path, '#');
        var queryIndex = IndexOf(path, '?');
        var paths = path[..(queryIndex ?? fragmentIndex ?? path.Length)];
        var query = queryIndex is null ? null : path[queryIndex.Value..(fragmentIndex ?? path.Length)];
        var fragment = fragmentIndex is null ? null : path[fragmentIndex.Value..];
        return From(paths, query, fragment);
    }

    static RelativeUri From(string path, string? query, string? fragment) {
        var @params = query is not null && query.StartsWith('?')
                          ? from i in query[1..].Split('&').Select(ParseQueryPairs)
                            group i.Value by i.Key into g
                            let multiValues = g.Where(v => v != null).ToImmutableHashSet()
                            select KeyValuePair.Create(g.Key, new StringValues(multiValues.ToArray()))
                          : [];
        return new(TiraxRelativeUri.SplitPaths(Uri.Unescape(path)).ToArray(),
                   @params.ToImmutableSortedDictionary(),
                   fragment is null? null : ExtractFragment(fragment));

    }

    static int? IndexOf(string path, char c) {
        var index = path.IndexOf(c);
        return index == -1 ? null : index;
    }

    #region Equality

    public virtual bool Equals(RelativeUri? other)
        => other is not null
        && (ReferenceEquals(this, other)
         || Paths.SequenceEqual(other.Paths)
         && QueryParams.SequenceEqual(other.QueryParams, QueryValueComparer.Instance)
         && Fragment == other.Fragment);

    [ExcludeFromCodeCoverage]
    public override int GetHashCode()
        => HashCode.Combine(Paths, QueryParams, Fragment);

    #endregion

    public override string ToString()
        => this.ApplyTo(new UriBuilder(null, null, 0)).ToString();

    internal static IEnumerable<string> ExpandQueryString(KeyValuePair<string, StringValues> kv) {
        if (kv.Value == StringValues.Empty)
            yield return Uri.Escape(kv.Key);
        else
            foreach(var v in kv.Value)
                yield return $"{Uri.Escape(kv.Key)}={Uri.Escape(v)}";
    }

    static string ExtractFragment(string fragment) => Uri.Unescape(fragment.StartsWith('#') ? fragment[1..] : fragment);

    static (string Key, string? Value) ParseQueryPairs(string queryParam){
        var splitPoint = queryParam.IndexOf('=');
        return splitPoint == -1
                   ? (Uri.Unescape(queryParam), null)
                   : (Uri.Unescape(queryParam[..splitPoint]), Uri.Unescape(queryParam[(splitPoint + 1)..]));
    }

    public class QueryValueComparer : IEqualityComparer<KeyValuePair<string, StringValues>>
    {
        public static readonly QueryValueComparer Instance = new();

        public bool Equals(KeyValuePair<string, StringValues> x, KeyValuePair<string, StringValues> y)
            => x.Key == y.Key && ((Object)x.Value).Equals(y.Value);

        public int GetHashCode(KeyValuePair<string, StringValues> obj)
            => HashCode.Combine(obj.Key, obj.Value);
    }
}

[PublicAPI]
public static class TiraxRelativeUri
{
    public static RelativeUri SetFragment(this RelativeUri uri, string? fragment = null)
        => uri with { Fragment = fragment };

    public static string[] SplitPaths(string path)
        => path.Split(Uri.PathSeparator).Select(s => s.Trim()).ToArray();

    public static RelativeUri ChangePath(this RelativeUri uri, string path){
        var replace = path.FirstOrDefault() == '/';
        var pathList = ValidatePathList(SplitPaths(path));
        var lhs = uri.Paths.Length > 0 && uri.Paths[^1] == string.Empty ? uri.Paths[..^1] : uri.Paths;
        var rhs = pathList.Length > 0 && pathList[0] == string.Empty ? pathList[1..] : pathList;
        return uri with { Paths = replace ? pathList : lhs.Concat(rhs).ToArray() };
    }

    static readonly Regex InvalidPathCharacters = new("[?#]", RegexOptions.Compiled);
    static string[] ValidatePathList(string[] pathList){
        var invalid = pathList.Select(s => InvalidPathCharacters.Match(s)).FirstOrDefault(i => i.Success);
        if (invalid is not null)
            throw new ArgumentException($"Path cannot contain {invalid.Value} at {invalid.Index}!");
        return pathList;
    }

    #region URI Query Parameters

    public static StringValues? Query(this RelativeUri uri, string key)
        => uri.QueryParams.TryGetValue(key, out var value) ? (StringValues?) value : null;

    public static RelativeUri RemoveQuery(this RelativeUri uri, string key)
        => uri with { QueryParams = uri.QueryParams.Remove(key) };

    public static RelativeUri ReplaceQuery(this RelativeUri uri, string key, StringValues? value = null)
        => uri with { QueryParams = uri.QueryParams.Remove(key).Add(key, value ?? StringValues.Empty) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelativeUri UpdateQuery(this RelativeUri uri, string key)
        => UpdateQuery3(uri, key, null);

    public static RelativeUri UpdateQuery<T>(this RelativeUri uri, string key, T value) where T : notnull
        => UpdateQuery3(uri, key, value switch {
            StringValues v => v,
            string v       => new StringValues(v),
            IEnumerable<string> v => new StringValues(v.ToArray()),
            ICollection v => new StringValues(v.OfType<object?>().Select(o => o?.ToString() ?? "null").ToArray()),

            _ => new StringValues(value.ToString())
        });

    static RelativeUri UpdateQuery3(this RelativeUri uri, string key, StringValues? value)
        => uri with {
            QueryParams = uri.QueryParams.TryGetValue(key, out var v)
                              ? uri.QueryParams.SetItem(key, new StringValues(v.Union(value ?? StringValues.Empty).ToArray()))
                              : value is null
                                  ? uri.QueryParams
                                  : uri.QueryParams.Add(key, value.Value)
        };

    public static RelativeUri UpdateQuery(this RelativeUri uri, params (string Key, StringValues Value)[] @params)
        => uri.UpdateQueries(@params);

    public static RelativeUri UpdateQueries(this RelativeUri uri, IEnumerable<KeyValuePair<string, StringValues>> queries)
        => uri.UpdateQueries(from kv in queries select (kv.Key, kv.Value));

    public static RelativeUri UpdateQueries(this RelativeUri uri, IEnumerable<(string Key, StringValues Value)> queries)
        => uri with { QueryParams = queries.Aggregate(uri.QueryParams, (last, i) => UpdateQuery(last, i.Key, i.Value)) };

    public static RelativeUri ClearQuery(this RelativeUri uri)
        => uri with { QueryParams = ImmutableSortedDictionary<string, StringValues>.Empty };

    static QueryParamType UpdateQuery(QueryParamType @params, string key, StringValues value) {
        var newValue = @params.TryGetValue(key, out var values)
                           ? new StringValues(values.Union(value).ToArray())
                           : value;
        return @params.SetItem(key, newValue);
    }

    #endregion

    public static UriBuilder ApplyTo(this RelativeUri uri, UriBuilder builder) {
        builder.Path = uri.PathOnly;
        builder.Query = string.Join('&', uri.QueryParams.SelectMany(RelativeUri.ExpandQueryString));
        builder.Fragment = Uri.Escape(uri.Fragment);
        return builder;
    }
}