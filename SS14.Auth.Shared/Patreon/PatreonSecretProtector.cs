using Microsoft.AspNetCore.DataProtection;

namespace SS14.Auth.Shared.Patreon;

public sealed class PatreonSecretProtector
{
    private readonly IDataProtector _protector;

    public PatreonSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SS14.Patreon.CampaignSecrets");
    }

    public string Protect(string plaintext)
    {
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedText)
    {
        return _protector.Unprotect(protectedText);
    }
}
