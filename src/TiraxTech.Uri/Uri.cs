﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExt;
using Microsoft.Extensions.Primitives;
using SystemUri = System.Uri;
using static LanguageExt.Prelude;

// ReSharper disable MemberCanBePrivate.Global

namespace TiraxTech;

public sealed record UriCredentials(string User, string Password);

public sealed record Uri(
    string Scheme,
    UriCredentials? Credentials,
    string Host,
    int? Port,
    Seq<string> Paths,
    Map<string, StringValues> QueryParams,
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

    static readonly IReadOnlyDictionary<string, int> DefaultPorts = new Dictionary<string, int>{
        { "http", 80 },
        { "ws", 80 },
        { "https", 443 },
        { "wss", 443 },
        { "ftp", 21 },
        { "ldap", 389 },
        { "net.tcp", 808 },
    }.ToImmutableDictionary();

    #region Parsing

    public static Uri From(string uri){
        var builder = new UriBuilder(uri);
        var credentials = builder.UserName.Length == 0 && builder.Password.Length == 0
                              ? null
                              : new UriCredentials(Unescape(builder.UserName), Unescape(builder.Password));
        var @params = builder.Query.StartsWith('?')
                          ? (from i in builder.Query[1..].Split('&').Select(ParseQueryPairs)
                             group i.Value by i.Key into g
                             let multiValues = g.Where(v => v != null).ToImmutableHashSet()
                             select KeyValuePair.Create(g.Key, new StringValues(multiValues.ToArray()))).ToMap()
                          : Empty;
        var defaultPort = DefaultPorts.TryGetValue(builder.Scheme).IfNone(-1);
        return new(builder.Scheme,
                   credentials,
                   Unescape(builder.Host),
                   builder.Port == -1 || builder.Port == defaultPort ? null : builder.Port,
                   SplitPaths(Unescape(builder.Path)),
                   @params,
                   ExtractFragment(builder.Fragment));
    }

    static (string Key, string? Value) ParseQueryPairs(string queryParam){
        var splitPoint = queryParam.IndexOf('=');
        return splitPoint == -1
                   ? (Unescape(queryParam), null)
                   : (Unescape(queryParam[..splitPoint]), Unescape(queryParam[(splitPoint + 1)..]));
    }

    #endregion

    public static implicit operator Uri(string uri) => From(uri);

    public Uri SetPort(int port) => this with { Port = port };
    public Uri RemovePort() => this with{ Port = null };
    public Uri SetFragment(string? fragment = null) => this with { Fragment = fragment };

#region Path methods

    static readonly Regex InvalidPathCharacters = new("[?#]", RegexOptions.Compiled);
    public Uri ChangePath(string path){
        var replace = path.FirstOrDefault() == '/';
        var pathList = ValidatePathList(SplitPaths(path));
        return this with { Paths = replace ? pathList : Paths.Append(pathList) };
    }

    public static string JoinPaths(IEnumerable<string> paths) => $"/{string.Join(PathSeparator, paths.Select(Escape))}";

    public string PathString() => JoinPaths(Paths);

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

    public Option<StringValues> Query(string key) => FindQuery(QueryParams, key).Map(i => i.Value);

    public Uri RemoveQuery(string key) => this with { QueryParams = QueryParams.Remove(key) };

    public Uri ReplaceQuery(string key, StringValues? value = null) => this with { QueryParams = QueryParams.Remove(key).Add(key, value ?? StringValues.Empty) };

    public Uri UpdateQuery(string key, StringValues? value = null) => this with{ QueryParams = UpdateQuery(QueryParams, key, value ?? StringValues.Empty) };
    public Uri UpdateQuery(params (string Key, StringValues Value)[] @params) => UpdateQueries(@params);

    public Uri UpdateQueries(IEnumerable<KeyValuePair<string, StringValues>> queries) =>
        this with { QueryParams = queries.Aggregate(QueryParams, (last, i) => UpdateQuery(last, i.Key, i.Value)) };
    public Uri UpdateQueries(IEnumerable<(string Key, StringValues Value)> queries) =>
        this with { QueryParams = queries.Aggregate(QueryParams, (last, i) => UpdateQuery(last, i.Key, i.Value)) };

    public Uri ClearQuery() => this with { QueryParams = Empty };

    static Map<string, StringValues> UpdateQuery(Map<string, StringValues> @params, string key, StringValues value) =>
        FindQuery(@params, key)
           .Match(existing => @params.SetItem((key), new StringValues(existing.Value.Union(value).ToArray())),
                  () => @params.Add(key, value));

    static Option<(string Key, StringValues Value)> FindQuery(in Map<string, StringValues> @params, string key) =>
        @params.Find(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

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
        builder.Query = string.Join('&', QueryParams.SelectMany(ExpandQueryString));
        return builder;
    }

    static IEnumerable<string> ExpandQueryString((string Key, StringValues Values) kv) {
        if (kv.Values == StringValues.Empty)
            yield return Escape(kv.Key);
        else
            foreach(var v in kv.Values)
                yield return $"{Escape(kv.Key)}={Escape(v)}";
    }

    static string Escape(string? s) => SystemUri.EscapeDataString(s ?? string.Empty);
    static string Unescape(string s) => SystemUri.UnescapeDataString(s);

#endregion
}