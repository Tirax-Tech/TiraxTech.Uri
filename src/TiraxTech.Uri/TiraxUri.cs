namespace TiraxTech;

[PublicAPI]
public static class TiraxUri
{
    #region URI Query Parameters

    extension(Uri uri)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringValues? Query(string key)
            => uri.Path.Query(key);

        public Uri RemoveQuery(string key)
            => uri with { Path = uri.Path.RemoveQuery(key) };

        public Uri ReplaceQuery(string key, StringValues? value = null)
            => uri with { Path = uri.Path.ReplaceQuery(key, value) };

        public Uri UpdateQuery<T>(string key, T value)
            => uri.UpdateQuery(key, value switch {
                StringValues v        => v,
                string v              => new StringValues(v),
                IEnumerable<string> v => new StringValues(v.ToArray()),
                ICollection v         => new StringValues(v.Cast<object?>().Select(o => o?.ToString() ?? "null").ToArray()),

                _ => value is null ? (StringValues?)null : new StringValues(value.ToString())
            });

        public Uri UpdateQuery(string key, StringValues? value = null)
            => uri with { Path = uri.Path.UpdateQuery(key, value) };

        public Uri UpdateQuery(params (string Key, StringValues Value)[] @params)
            => uri with { Path = uri.Path.UpdateQuery(@params) };

        public Uri UpdateQueries(IEnumerable<KeyValuePair<string, StringValues>> queries)
            => uri with { Path = uri.Path.UpdateQueries(queries) };

        public Uri UpdateQueries(IEnumerable<(string Key, StringValues Value)> queries)
            => uri with { Path = uri.Path.UpdateQueries(queries) };

        public Uri ClearQuery()
            => uri with { Path = uri.Path.ClearQuery() };
    }

    #endregion
}