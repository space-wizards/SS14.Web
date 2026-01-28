#nullable enable
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using SS14.Web.OpenId.Configuration;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace SS14.Web.OpenId.EventHandlers;

public class TokenSigningHandler : IOpenIddictServerHandler<GenerateTokenContext>
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    private readonly OpenIdCertificateConfiguration _config = new();

    public TokenSigningHandler(IOpenIddictApplicationManager applicationManager, IConfiguration configuration)
    {
        _applicationManager = applicationManager;

        configuration.GetSection("OpenId").GetSection(OpenIdCertificateConfiguration.Name).Bind(_config);
    }

    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<GenerateTokenContext>()
            .UseScopedHandler<TokenSigningHandler>()
            .SetOrder(OpenIddictServerHandlers.Protection.AttachSecurityCredentials.Descriptor.Order + 500)
            .Build();

    public async ValueTask HandleAsync(GenerateTokenContext context)
    {
        if (context.Request?.ClientId is null)
            return;

        // Ignore client assertion
        if (context.TokenType is OpenIddictConstants.TokenTypeIdentifiers.Private.ClientAssertion)
            return;

        var app = await _applicationManager.FindByClientIdAsync(context.Request.ClientId);
        if (app is null)
            return;

        var settings = await _applicationManager.GetSettingsAsync(app);

        var encryptionAlgorithm = settings.TryGetValue(OpenIdConstants.EncryptionAlgorithmSetting, out var encryptionAlg)
            ? encryptionAlg
            : _config.DefaultEncryptionAlgorithm;

        encryptionAlgorithm = string.IsNullOrEmpty(encryptionAlgorithm) ? _config.DefaultEncryptionAlgorithm : encryptionAlgorithm; ;

        if (encryptionAlgorithm is not null)
        {
            foreach (var credential in context.Options.EncryptionCredentials)
            {
                if (!credential.Enc.Equals(encryptionAlgorithm))
                    continue;

                context.EncryptionCredentials = credential;
                break;
            }
        }

        var signingAlgorithm = settings.TryGetValue(OpenIdConstants.SigningAlgorithmSetting, out var signingAlg)
            ? signingAlg
            : _config.DefaultSigningAlgorithm;

        signingAlgorithm = string.IsNullOrEmpty(signingAlgorithm) ? _config.DefaultSigningAlgorithm : signingAlgorithm;

        if (signingAlgorithm is not null)
        {
            foreach (var credential in context.Options.SigningCredentials)
            {
                if (!credential.Algorithm.Equals(signingAlgorithm))
                    continue;

                context.SigningCredentials = credential;
                break;
            }
        }
    }
}
