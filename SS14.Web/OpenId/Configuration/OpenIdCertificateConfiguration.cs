#nullable enable
using System.Collections.Generic;

namespace SS14.Web.OpenId.Configuration;

public sealed class OpenIdCertificateConfiguration
{
    public const string Name = "Certificates";

    /// <summary>
    /// Sets the default encryption algorithm if it didn't get overriden by the application.
    /// If this is null the first available certificate will be used.
    /// </summary>
    /// <remarks>
    /// A certificate with the matching algorithm needs to be present
    /// </remarks>
    public string? DefaultEncryptionAlgorithm { get; set; }

    /// <summary>
    /// Sets the default encryption algorithm if it didn't get overriden by the application.
    /// If this is null the first available certificate will be used.
    /// </summary>
    /// <remarks>
    /// A certificate with the matching algorithm needs to be present
    /// </remarks>
    public string? DefaultSigningAlgorithm { get; set; }

    public List<CertificateOptions> EncryptionCertificates { get; set; } = [];
    public List<CertificateOptions> SigningCertificates { get; set; } = [];
}
