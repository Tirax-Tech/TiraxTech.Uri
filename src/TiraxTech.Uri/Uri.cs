using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using SystemUri = System.Uri;

// ReSharper disable MemberCanBePrivate.Global

namespace TiraxTech;

public sealed record UriCredentials(string User, string Password);

public sealed record Uri(
    string Scheme,
    UriCredentials? Credentials,
    string Host,
    int? Port,
    RelativeUri Path
)
{
    public const char PathSeparator = '/';

    public static readonly GenericUriBuilder Http = new("http");
    public static readonly GenericUriBuilder Https = new("https");
    public static readonly GenericUriBuilder Ws = new("ws");
    public static readonly GenericUriBuilder Wss = new("wss");
    public static readonly GenericUriBuilder Ftp = new("ftp");
    public static readonly GenericUriBuilder Ldap = new("ldap");
    public static readonly GenericUriBuilder NetTcp = new("net.tcp");
    public static readonly GenericUriBuilder NetPipe = new("net.pipe");
    public static readonly FileUriBuilder File = new();

    static readonly IReadOnlyDictionary<string, int> DefaultPorts = new Dictionary<string, int>{
        { "http", 80 },
        { "ws", 80 },
        { "https", 443 },
        { "wss", 443 },
        { "ftp", 21 },
        { "ldap", 389 },
        { "net.tcp", 808 },
    }.ToImmutableDictionary();

    public static Uri From(string uri){
        var builder = new UriBuilder(uri);
        var credentials = builder.UserName.Length == 0 && builder.Password.Length == 0
                              ? null
                              : new UriCredentials(Unescape(builder.UserName), Unescape(builder.Password));
        var defaultPort = DefaultPorts.GetValueOrDefault(builder.Scheme, -1);
        return new(builder.Scheme,
                   credentials,
                   Unescape(builder.Host),
                   builder.Port == -1 || builder.Port == defaultPort ? null : builder.Port,
                   RelativeUri.From(builder));
    }

    public static implicit operator Uri(string uri) => From(uri);

    public Uri SetPort(int port) => this with { Port = port };
    public Uri RemovePort() => this with{ Port = null };

    public Uri SetFragment(string? fragment = null) => this with { Path = Path.SetFragment(fragment) };

#region Path methods

    public Uri ChangePath(string path) => this with { Path = Path.ChangePath(path) };

    public static string JoinPaths(IEnumerable<string> paths) => $"{PathSeparator}{string.Join(PathSeparator, paths.Select(Escape))}";

    [Obsolete("Use Path.PathOnly instead.")]
    public string PathString() => Path.PathOnly;

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

#region ToString
    public override string ToString() => CreateUriBuilder().ToString();
    public System.Uri ToSystemUri() => CreateUriBuilder().Uri;

    UriBuilder CreateUriBuilder(){
        var builder = new UriBuilder(Scheme, Host)
        { UserName = Escape(Credentials?.User),
          Password = Escape(Credentials?.Password),
          Path     = Path.PathOnly,
          Fragment = Escape(Path.Fragment) };
        if (Port != null)
            builder.Port = Port.Value;
        builder.Query = string.Join('&', Path.QueryParams.SelectMany(ExpandQueryString));
        return builder;
    }

    static IEnumerable<string> ExpandQueryString(KeyValuePair<string, StringValues> kv) {
        if (kv.Value == StringValues.Empty)
            yield return Escape(kv.Key);
        else
            foreach(var v in kv.Value)
                yield return $"{Escape(kv.Key)}={Escape(v)}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Escape(string? s) => SystemUri.EscapeDataString(s ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Unescape(string s) => SystemUri.UnescapeDataString(s);

#endregion
}

[PublicAPI]
public static class TiraxUri
{
    #region URI Query Parameters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringValues? Query(this Uri uri, string key)
        => uri.Path.Query(key);

    public static Uri RemoveQuery(this Uri uri, string key)
        => uri with { Path = uri.Path.RemoveQuery(key) };

    public static Uri ReplaceQuery(this Uri uri, string key, StringValues? value = null)
        => uri with { Path = uri.Path.ReplaceQuery(key, value) };

    public static Uri UpdateQuery(this Uri uri, string key, StringValues? value = null)
        => uri with{ Path = uri.Path.UpdateQuery(key, value) };

    public static Uri UpdateQuery(this Uri uri, params (string Key, StringValues Value)[] @params)
        => uri with { Path = uri.Path.UpdateQuery(@params) };

    public static Uri UpdateQueries(this Uri uri, IEnumerable<KeyValuePair<string, StringValues>> queries)
        => uri with { Path = uri.Path.UpdateQueries(queries) };

    public static Uri UpdateQueries(this Uri uri, IEnumerable<(string Key, StringValues Value)> queries)
        => uri with { Path = uri.Path.UpdateQueries(queries) };

    public static Uri ClearQuery(this Uri uri)
        => uri with { Path = uri.Path.ClearQuery() };

    #endregion
}