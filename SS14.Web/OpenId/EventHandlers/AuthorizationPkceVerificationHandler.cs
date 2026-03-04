#nullable enable
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using SS14.Web.OpenId.Extensions;
using SS14.Web.OpenId.Services;

namespace SS14.Web.OpenId.EventHandlers;

public sealed class AuthorizationPkceVerificationHandler : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>
{
    private readonly SpaceApplicationManager _applicationManager;

    public AuthorizationPkceVerificationHandler(SpaceApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ValidateAuthorizationRequestContext>()
            .UseScopedHandler<AuthorizationPkceVerificationHandler>()
            .SetOrder(int.MaxValue - 500)
            .Build();

    public async ValueTask HandleAsync(OpenIddictServerEvents.ValidateAuthorizationRequestContext context)
    {
        var app = await _applicationManager.FindByClientIdAsync(context.Request.ClientId!);
        if (app is null)
            return;

        var method = context.Request.CodeChallengeMethod;
        if (await _applicationManager.GetAllowPlainPkceSetting(app))
            return;

        if (method is OpenIddictConstants.CodeChallengeMethods.Plain)
            context.Reject(error: OpenIddictConstants.Errors.InvalidRequest, description: "Plain PKCE not allowed.");
    }
}
