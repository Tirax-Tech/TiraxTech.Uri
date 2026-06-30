using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using RZ.Foundation;
using RZ.Foundation.Types;
using SystemUri = System.Uri;
using static RZ.Foundation.AOT.Prelude;

// ReSharper disable MemberCanBePrivate.Global

namespace TiraxTech;

/// <summary>
/// Error codes produced by the <see cref="Uri"/> API when an operation fails.
/// </summary>
[PublicAPI]
public static class UriError
{
    public const string Parse = "uri.parse";
    public const string InvalidPathChar = "uri.path.invalid-char";
    public const string PasswordRequired = "uri.credentials.password-required";
    public const string UserRequired = "uri.credentials.user-required";
}

public sealed record UriCredentials(string User, string Password)
{
    // Redact the password so it is never leaked via ToString()/logging (defect #9).
    public override string ToString() => $"{nameof(UriCredentials)} {{ User = {User}, Password = *** }}";
}

[PublicAPI]
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

    // Rendered for an instance that was corrupted via raw `with` (e.g. an invalid Host); construction
    // through From/the builders/SetCredentials/ChangePath always yields a renderable value.
    const string InvalidUriText = "about:invalid";
    static readonly SystemUri InvalidSystemUri = new(InvalidUriText);

    static readonly IReadOnlyDictionary<string, int> DefaultPorts = new Dictionary<string, int> {
        { "http", 80 },
        { "ws", 80 },
        { "https", 443 },
        { "wss", 443 },
        { "ftp", 21 },
        { "ldap", 389 },
        { "net.tcp", 808 },
        { "net.pipe", 808 },
    }.ToImmutableDictionary();

    /// <summary>
    /// Parse a URI string.
    /// </summary>
    /// <remarks>
    /// Returns a failure (code <see cref="UriError.Parse"/>) for malformed input rather than throwing.
    /// Note that dangerous schemes (e.g. <c>javascript:</c>) and dot-segments (<c>..</c>) are accepted as-is —
    /// the library intentionally supports arbitrary/custom schemes; consumers are responsible for vetting
    /// untrusted input and for the path normalisation that <see cref="ToSystemUri"/> applies.
    /// </remarks>
    public static Outcome<Uri> From(string uri) {
        if (Fail(TryCatch(() => new UriBuilder(uri)), out var e, out var builder))
            return ErrorInfo.New(UriError.Parse, $"Invalid URI: '{uri}'", innerError: e);

        if (FailButNotFound(BuildCredentials(builder), out e, out var credentials)) return e.Trace();

        return new Uri(builder.Scheme,
                       credentials,
                       Unescape(builder.Host),
                       NormalizePort(builder),
                       RelativeUri.From(builder));
    }

    static Outcome<UriCredentials> BuildCredentials(UriBuilder builder)
        => builder.UserName.Length == 0 && builder.Password.Length == 0
               ? ErrorInfo._NotFound()
               : ValidateCredentials(Unescape(builder.UserName), Unescape(builder.Password));

    static int? NormalizePort(UriBuilder builder) {
        var defaultPort = DefaultPorts.GetValueOrDefault(builder.Scheme, -1);
        return builder.Port == -1 || builder.Port == defaultPort ? null : builder.Port;
    }

    public Uri SetPort(int port) => this with { Port = port };
    public Uri RemovePort()      => this with { Port = null };

    public Uri SetFragment(string? fragment = null) => this with { Path = Path.SetFragment(fragment) };

    #region Path methods

    /// <summary>
    /// Append <paramref name="path"/> to the current path. Fails (code <see cref="UriError.InvalidPathChar"/>)
    /// if a segment contains <c>?</c> or <c>#</c>.
    /// </summary>
    /// <remarks>
    /// Dot-segments (<c>.</c>/<c>..</c>) are <b>not</b> rejected; <see cref="ToSystemUri"/> applies RFC 3986
    /// normalisation, so appending untrusted <c>..</c> segments can traverse above the intended base path.
    /// </remarks>
    public Outcome<Uri> ChangePath(string path)
        => Path.ChangePath(path).Map(p => this with { Path = p });

    public Uri ChangePath(RelativeUri path) => this with { Path = path };

    public static string JoinPaths(IEnumerable<string> paths)
        => string.Join(PathSeparator, paths.Select(Escape));

    [Obsolete("Use Path.PathOnly instead.")]
    public string PathString() => Path.PathOnly;

    #endregion

    #region User Credentials

    /// <summary>
    /// Shortcut for setting credentials.
    /// </summary>
    /// <param name="user">User name can be <c>null</c> to clear credentials.</param>
    /// <param name="password">Password can be <c>null</c> only if <paramref name="user"/> is also <c>null</c></param>
    /// <returns>New <see cref="Uri"/> with credentials set or cleared, or a failure if the pair is invalid.</returns>
    public Outcome<Uri> SetCredentials(string? user = null, string? password = null) {
        if (FailButNotFound(ValidateCredentials(user, password), out var e, out var creds)) return e.Trace();
        return this with { Credentials = creds };
    }

    /// <summary>
    /// Create a valid <see cref="UriCredentials"/> from the given user/password pair.
    /// </summary>
    /// <param name="user">User name can be <c>null</c> to clear credentials, but must be non-empty otherwise.</param>
    /// <param name="password">Password can be <c>null</c> only if <paramref name="user"/> is also <c>null</c>.</param>
    /// <returns>
    /// The <see cref="UriCredentials"/> on success. Returns a <c>NotFound</c> failure when <paramref name="user"/>
    /// is <c>null</c> — this signals "no credentials" and is a normal result rather than an error, so handle it
    /// with <c>FailButNotFound</c>/<c>IfNotFound</c>. Fails with <see cref="UriError.UserRequired"/> for an empty
    /// user, or <see cref="UriError.PasswordRequired"/> when a user is set but the password is <c>null</c>.
    /// </returns>
    public static Outcome<UriCredentials> ValidateCredentials(string? user = null, string? password = null) {
        if (user is null) return ErrorInfo._NotFound();
        if (user.Length == 0) return new ErrorInfo(UriError.UserRequired, "User name cannot be empty when credentials are set.");
        if (password is null) return new ErrorInfo(UriError.PasswordRequired, "Password cannot be null when a user is set.");
        return new UriCredentials(user, password);
    }

    #endregion

    #region ToString

    public override string ToString()
        => TryCatch(() => CreateUriBuilder().ToString()).IfFail(_ => InvalidUriText);

    public SystemUri ToSystemUri()
        => TryCatch(() => CreateUriBuilder().Uri).IfFail(_ => InvalidSystemUri);

    static readonly char[] InvalidHostChars = ['/', '\\', '?', '#', '@', ' '];

    UriBuilder CreateUriBuilder() {
        if (Host.IndexOfAny(InvalidHostChars) >= 0)
            throw new FormatException($"Invalid host: '{Host}'");
        var builder = Path.ApplyTo(new UriBuilder(Scheme, Host) {
            UserName = Escape(Credentials?.User),
            Password = Escape(Credentials?.Password)
        });
        if (Port != null)
            builder.Port = Port.Value;
        return builder;
    }

    static IEnumerable<string> ExpandQueryString(KeyValuePair<string, StringValues> kv) {
        if (kv.Value == StringValues.Empty)
            yield return Escape(kv.Key);
        else
            foreach (var v in kv.Value)
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
        => uri with { Path = uri.Path.UpdateQuery(key, value) };

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