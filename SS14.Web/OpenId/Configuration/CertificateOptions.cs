#nullable enable
namespace SS14.Web.OpenId.Configuration;

public sealed class CertificateOptions
{
    public string Path { get; set; } = null!;
    public string? Password { get; set; }
    public string? Algorithm { get; set; }
}
