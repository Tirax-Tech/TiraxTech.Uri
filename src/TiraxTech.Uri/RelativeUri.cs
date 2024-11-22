using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using QueryParamType = System.Collections.Immutable.ImmutableSortedDictionary<string, Microsoft.Extensions.Primitives.StringValues>;

namespace TiraxTech;

public record RelativeUri(
    string[] Paths,
    QueryParamType QueryParams,
    string? Fragment
)
{
    public string PathOnly => Uri.JoinPaths(Paths);

    public static RelativeUri From(UriBuilder builder) {
        var @params = builder.Query.StartsWith('?')
                          ? from i in builder.Query[1..].Split('&').Select(ParseQueryPairs)
                            group i.Value by i.Key into g
                            let multiValues = g.Where(v => v != null).ToImmutableHashSet()
                            select KeyValuePair.Create(g.Key, new StringValues(multiValues.ToArray()))
                          : [];
        return new(TiraxRelativeUri.SplitPaths(Uri.Unescape(builder.Path)).ToArray(),
                   @params.ToImmutableSortedDictionary(),
                   ExtractFragment(builder.Fragment));
    }

    #region Equality

    public virtual bool Equals(RelativeUri? other)
        => other is not null
        && (ReferenceEquals(this, other)
         || Paths.SequenceEqual(other.Paths)
         && QueryParams.SequenceEqual(other.QueryParams, QueryValueComparer.Instance)
         && Fragment == other.Fragment);

    public override int GetHashCode() => HashCode.Combine(Paths, QueryParams, Fragment);

    #endregion

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

        public bool Equals(KeyValuePair<string, StringValues> x, KeyValuePair<string, StringValues> y) {
            return x.Key == y.Key && ((Object)x.Value).Equals(y.Value);
        }

        public int GetHashCode(KeyValuePair<string, StringValues> obj) {
            return HashCode.Combine(obj.Key, obj.Value);
        }
    }
}

[PublicAPI]
public static class TiraxRelativeUri
{
    public static RelativeUri SetFragment(this RelativeUri uri, string? fragment = null)
        => uri with { Fragment = fragment };

    public static string[] SplitPaths(string path)
        => path.Split(Uri.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    public static RelativeUri ChangePath(this RelativeUri uri, string path){
        var replace = path.FirstOrDefault() == '/';
        var pathList = ValidatePathList(SplitPaths(path));
        return uri with { Paths = replace ? pathList : uri.Paths.Concat(pathList).ToArray() };
    }

    static readonly Regex InvalidPathCharacters = new("[?#]", RegexOptions.Compiled);
    static string[] ValidatePathList(string[] pathList){
        var invalid = pathList.Select(s => InvalidPathCharacters.Match(s)).FirstOrDefault(i => i.Success);
        if (invalid != null)
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

    public static RelativeUri UpdateQuery(this RelativeUri uri, string key, StringValues? value = null)
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

}