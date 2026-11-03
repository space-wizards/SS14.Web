#nullable enable
using System.Collections.Generic;
using JetBrains.Annotations;

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
    [UsedImplicitly]
    public string DefaultEncryptionAlgorithm { get; init; } = null!;

    /// <summary>
    /// Sets the default encryption algorithm if it didn't get overriden by the application.
    /// If this is null the first available certificate will be used.
    /// </summary>
    /// <remarks>
    /// A certificate with the matching algorithm needs to be present
    /// </remarks>
    [UsedImplicitly]
    public string DefaultSigningAlgorithm { get; init; } = null!;

    public List<CertificateOptions> EncryptionCertificates { get; init; } = [];
    public List<CertificateOptions> SigningCertificates { get; init; } = [];
}
