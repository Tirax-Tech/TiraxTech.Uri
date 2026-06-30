namespace TiraxTech;

public sealed class GenericUriBuilder(string scheme)
{
    public Outcome<Uri> Host(string host) => Uri.From($"{scheme}://{System.Uri.EscapeDataString(host)}");
}

public sealed class FileUriBuilder
{
    public Outcome<Uri> Host(string? host = null) => Uri.From($"file://{System.Uri.EscapeDataString(host ?? string.Empty)}/");
}
