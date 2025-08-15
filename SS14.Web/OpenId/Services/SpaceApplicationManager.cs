#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using SS14.Auth.Shared.Data;
using SS14.Web.Models.Types;

namespace SS14.Web.OpenId.Services;

public class SpaceApplicationManager(
    IOpenIddictApplicationCache<SpaceApplication> cache,
    ILogger<OpenIddictApplicationManager<SpaceApplication>> logger,
    IOptionsMonitor<OpenIddictCoreOptions> options,
    IOpenIddictApplicationStore<SpaceApplication> store)
    : OpenIddictApplicationManager<SpaceApplication>(cache, logger, options, store)
{
    private const string LegacySecretPrefix = "_OLD_";

    public ClientSecretInfo? FindSecretById(SpaceApplication app,
        int id,
        CancellationToken ct = default)
    {
        if (app.ClientSecret is null)
            return null;

        var span = app.ClientSecret.AsSpan();
        var secrets = span.SplitAny(',', '.');
        while (secrets.MoveNext())
        {
            if (int.Parse(span[secrets.Current]) == id)
                return ParseSecretInfo(span, secrets);

            secrets.MoveNext(); //timestamp
            secrets.MoveNext(); //key
            secrets.MoveNext(); //description
        }

        return null;
    }

    public List<ClientSecretInfo> ListSecrets(SpaceApplication app,
        CancellationToken ct = default)
    {
        var result = new List<ClientSecretInfo>();
        if (app.ClientSecret is null)
            return result;

        var span = app.ClientSecret.AsSpan();
        var secrets = span.SplitAny(',', '.');
        while (secrets.MoveNext())
        {
            result.Add(ParseSecretInfo(span, secrets));
        }

        return result;
    }

    // MoveNext() needs to have been called for parts once already. This is to allow checking the client secret id
    private ClientSecretInfo ParseSecretInfo(ReadOnlySpan<char> span, MemoryExtensions.SpanSplitEnumerator<char> parts)
    {
        var id = span[parts.Current];
        parts.MoveNext();
        var timestamp = span[parts.Current];
        parts.MoveNext();
        var isLegacy = span[parts.Current];
        parts.MoveNext();
        var description = span[parts.Current];

        return new ClientSecretInfo(
            int.Parse(id),
            DateTime.Parse(timestamp),
            description.ToString(),
            isLegacy.StartsWith(LegacySecretPrefix)
        );
    }

    protected override async ValueTask<string> ObfuscateClientSecretAsync(string secret, CancellationToken ct = new())
    {
        var legacy = secret.StartsWith(LegacySecretPrefix);
        if (legacy)
            secret = secret[5..];

        var secrets = secret.Split(',');
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var obfuscatedSecrets = new string[secrets.Length];

        for (var i = 0; i < secrets.Length; i++)
        {
            // Legacy secrets are from a migration and are already hashed
            var obfuscatedSecret = !legacy
                ? await base.ObfuscateClientSecretAsync(secrets[i], ct)
                : $"{LegacySecretPrefix}{secrets[i]}";
            var description = legacy ? "legacy-please-regenerate" : secrets[i][^6..];
            obfuscatedSecrets[i] = $"{i}.{timestamp}.{obfuscatedSecret}.{description}";
        }

        return string.Join(",", obfuscatedSecrets);
    }

    protected override async ValueTask<bool> ValidateClientSecretAsync(string secret,
        string comparand,
        CancellationToken ct = new())
    {
        var secrets = secret.Split(',');
        foreach (var pair in secrets)
        {
            var value = pair.Split('.')[2];
            var legacy = value.StartsWith(LegacySecretPrefix);

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (legacy)
                value = value[5..];

            if (!legacy && await base.ValidateClientSecretAsync(value, comparand, ct))
                return true;

            if (legacy && await ValidateLegacySecret(value, comparand, ct))
                return true;
        }

        return false;
    }

    private async ValueTask<bool> ValidateLegacySecret(string value, string comparand, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
