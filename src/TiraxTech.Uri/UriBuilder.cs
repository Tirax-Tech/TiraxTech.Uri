namespace TiraxTech;

public sealed class GenericUriBuilder
{
    readonly string scheme;
    public GenericUriBuilder(string scheme){
        this.scheme = scheme;
    }

    public Uri Host(string host) => Uri.From($"{scheme}://{System.Uri.EscapeDataString(host)}");
}

public sealed class FileUriBuilder
{
    public Uri Host(string? host = null) => Uri.From($"file://{System.Uri.EscapeDataString(host ?? string.Empty)}/");
}
