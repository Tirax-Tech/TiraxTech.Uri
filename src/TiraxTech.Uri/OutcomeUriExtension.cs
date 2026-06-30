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
    // Total operations compose with Map.
    extension(Outcome<Uri> uri)
    {
        public Outcome<Uri> ChangePath(string path)
            => uri.Bind(u => u.ChangePath(path));

        public Outcome<Uri> SetCredentials(string? user = null, string? password = null)
            => uri.Bind(u => u.SetCredentials(user, password));

        public Outcome<Uri> ChangePath(RelativeUri path)
            => uri.Map(u => u.ChangePath(path));

        public Outcome<Uri> SetPort(int port)
            => uri.Map(u => u.SetPort(port));

        public Outcome<Uri> RemovePort()
            => uri.Map(u => u.RemovePort());

        public Outcome<Uri> SetFragment(string? fragment = null)
            => uri.Map(u => u.SetFragment(fragment));

        public Outcome<Uri> RemoveQuery(string key)
            => uri.Map(u => u.RemoveQuery(key));

        public Outcome<Uri> ReplaceQuery(string key, StringValues? value = null)
            => uri.Map(u => u.ReplaceQuery(key, value));

        public Outcome<Uri> UpdateQuery(string key, StringValues? value = null)
            => uri.Map(u => u.UpdateQuery(key, value));

        public Outcome<Uri> UpdateQuery(params (string Key, StringValues Value)[] @params)
            => uri.Map(u => u.UpdateQuery(@params));

        public Outcome<Uri> UpdateQueries(IEnumerable<KeyValuePair<string, StringValues>> queries)
            => uri.Map(u => u.UpdateQueries(queries));

        public Outcome<Uri> UpdateQueries(IEnumerable<(string Key, StringValues Value)> queries)
            => uri.Map(u => u.UpdateQueries(queries));

        public Outcome<Uri> ClearQuery()
            => uri.Map(u => u.ClearQuery());

        public Outcome<UriCache> Cached()
            => uri.Map(u => u.Cached());
    }
}
