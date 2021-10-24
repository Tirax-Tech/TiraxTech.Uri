using System.Text.RegularExpressions;
using LanguageExt;
using static LanguageExt.Prelude;
using SystemUri = System.Uri;

// ReSharper disable MemberCanBePrivate.Global

namespace TiraxTech;

public sealed record UriCredentials(string User, string Password);

public sealed record Uri(
    string Scheme,
    UriCredentials? Credentials,
    string Host,
    int? Port,
    Seq<string> Paths,
    Set<(string Key, string Value)> QueryParams,
    string? Fragment
)
{
    const char PathSeparator = '/';

    public static readonly GenericUriBuilder Http = new("http");
    public static readonly GenericUriBuilder Https = new("https");
    public static readonly GenericUriBuilder Ws = new("ws");
    public static readonly GenericUriBuilder Wss = new("wss");
    public static readonly GenericUriBuilder Ftp = new("ftp");
    public static readonly GenericUriBuilder Ldap = new("ldap");
    public static readonly GenericUriBuilder NetTcp = new("net.tcp");
    public static readonly GenericUriBuilder NetPipe = new("net.pipe");
    public static readonly FileUriBuilder File = new();
    
    public static Uri From(string uri){
        var builder = new UriBuilder(uri);
        var credentials = builder.UserName.Length == 0 && builder.Password.Length == 0
                              ? null
                              : new UriCredentials(Unescape(builder.UserName), Unescape(builder.Password));
        var @params = builder.Query.StartsWith('?')
                          ? toSet(builder.Query[1..].Split('&').Select(ParseQueryPairs))
                          : Empty;
        return new Uri(builder.Scheme,
                       credentials,
                       Unescape(builder.Host),
                       builder.Port == -1 ? null : builder.Port,
                       SplitPaths(Unescape(builder.Path)),
                       @params,
                       ExtractFragment(builder.Fragment));
    }

    public static implicit operator Uri(string uri) => From(uri);

    public Uri SetPort(int port) => this with { Port = port };
    public Uri SetFragment(string? fragment = null) => this with { Fragment = fragment };
    
    public UriCache Cache() => UriCache.From(this);

#region Path methods

    static readonly Regex InvalidPathCharacters = new("[?#]", RegexOptions.Compiled);
    public Uri ChangePath(string path){
        var replace = path.FirstOrDefault() == '/';
        var pathList = ValidatePathList(SplitPaths(path));
        return this with { Paths = replace ? pathList : Paths.Append(pathList) };
    }

    public static string JoinPaths(IEnumerable<string> paths) => $"/{string.Join(PathSeparator, paths.Select(Escape))}";
    
    public static Seq<string> SplitPaths(string path) => SplitPathEnum(path).ToSeq();
    static IEnumerable<string> SplitPathEnum(string path) => path.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    static Seq<string> ValidatePathList(Seq<string> pathList){
        var invalid = pathList.Select(InvalidPathCharacters.Match).FirstOrDefault(i => i.Success);
        if (invalid != null)
            throw new ArgumentException($"Path cannot contain {invalid.Value} at {invalid.Index}!");
        return pathList;
    }

#endregion

#region URI Query Parameters

    public string? Query(string key) => FindQuery(QueryParams, key)?.Value;

    public Uri SetQuery(string key, string? value = null) => this with { QueryParams = UpdateQuery(QueryParams, key, value) };

    public Uri SetQuery(params (string Key, string Value)[] @params) =>
        this with { QueryParams = @params.Aggregate(QueryParams, (last, i) => UpdateQuery(last, i.Key, i.Value)) };
    
    public Uri ClearQuery() => this with { QueryParams = Empty };

    static (string Key,string Value) ParseQueryPairs(string queryParam){
        var splitPoint = queryParam.IndexOf('=');
        return splitPoint == -1
                   ? (Unescape(queryParam), string.Empty)
                   : (Unescape(queryParam[..splitPoint]), Unescape(queryParam[(splitPoint + 1)..]));
    }

    static Set<(string Key, string Value)> UpdateQuery(in Set<(string Key, string Value)> @params, string key, string? value){
        var item = FindQuery(@params, key);
        var q = @params;
        if (item != null) q = q.Remove(item.Value);
        if (value != null) q = q.Add((key, value));
        return q;
    }

    static (string Key, string Value)? FindQuery(in Set<(string Key, string Value)> @params, string key) =>
        @params.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

#endregion

#region User Credentials

    /// <summary>
    /// Shortcut for setting credentials.
    /// </summary>
    /// <param name="user">User name can be <c>null</c> to clear credentials.</param>
    /// <param name="password">Password can be <c>null</c> only if <paramref name="user"/> is also <c>null</c></param>
    /// <returns>New <see cref="Uri"/> with credentials set or cleared depending on the value of <paramref name="user"/>.</returns>
    public Uri SetCredentials(string? user = null, string? password = null) => this with { Credentials = ValidateCredentials(user, password) };

    /// <summary>
    /// Create a valid <see cref="UriCredentials"/> from the given user/password pairs.
    /// </summary>
    /// <param name="user">User name can be <c>null</c> to clear credentials.</param>
    /// <param name="password">Password can be <c>null</c> only if <paramref name="user"/> is also <c>null</c></param>
    /// <returns>Unless <paramref name="user"/> is <c>null</c>, the method returns <see cref="UriCredentials"/> that represents user/password
    /// for <see cref="Uri"/>.</returns>
    /// <exception cref="ArgumentException">when <paramref name="password"/> is <c>null</c> but <paramref name="user"/> has a value.</exception>
    public static UriCredentials? ValidateCredentials(string? user = null, string? password = null){
        if (user != null && password == null)
            throw new ArgumentException("Password cannot be null!");
        return user == null ? null : new UriCredentials(user, password!);
    }

#endregion

    static string ExtractFragment(string fragment) => Unescape(fragment.StartsWith('#') ? fragment[1..] : fragment);
    
#region ToString
    public override string ToString() => CreateUriBuilder().ToString();
    public System.Uri ToSystemUri() => CreateUriBuilder().Uri;

    UriBuilder CreateUriBuilder(){
        var builder = new UriBuilder(Scheme, Host)
        { UserName = Escape(Credentials?.User),
          Password = Escape(Credentials?.Password),
          Path     = JoinPaths(Paths),
          Fragment = Escape(Fragment) };
        if (Port != null)
            builder.Port = Port.Value;
        builder.Query = string.Join('&', QueryParams.Select(kv => $"{Escape(kv.Key)}={Escape(kv.Value)}"));
        return builder;
    }

    static string Escape(string? s) => SystemUri.EscapeDataString(s ?? string.Empty);
    static string Unescape(string s) => SystemUri.UnescapeDataString(s);

#endregion
}
