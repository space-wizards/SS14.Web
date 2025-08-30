#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

    public ClientSecretInfo? FindSecretById(SpaceApplication app, int id, CancellationToken ct = default)
    {
        return app.ClientSecret is null ? null : Functions.FindById(app.ClientSecret, id);
    }

    public List<ClientSecretInfo> ListSecrets(SpaceApplication app, CancellationToken ct = default)
    {
        var result = new List<ClientSecretInfo>();
        if (app.ClientSecret is null)
            return result;

        var span = app.ClientSecret.AsSpan();
        var secrets = span.SplitAny(',', '.');
        while (secrets.MoveNext())
        {
            result.Add(Functions.ParseSecretInfo(span, ref secrets));
        }

        return result;
    }

    public async ValueTask<ClientSecretInfo> AddSecret(SpaceApplication app, string secret, string? desc = null, CancellationToken ct = default)
    {
        var key = await base.ObfuscateClientSecretAsync(secret, ct);
        var (secretsString, info) = Functions.AddSecret(app.ClientSecret, key, desc ?? secret[^6..]);
        await Store.SetClientSecretAsync(app, secretsString, ct);
        await base.UpdateAsync(app, ct);
        return info;
    }

    public async ValueTask RemoveSecret(SpaceApplication app, int id, CancellationToken ct = default)
    {
        if (app.ClientSecret is null)
            return;

        var result = Functions.RemoveSecret(app.ClientSecret, id);
        if (result == app.ClientSecret)
            return;

        await Store.SetClientSecretAsync(app, result, ct);
        await base.UpdateAsync(app, ct);
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

            if (legacy && Functions.ValidateLegacySecret(value, comparand))
                return true;
        }

        return false;
    }

    public static class Functions
    {
        // MoveNext() needs to have been called for parts once already. This is to allow checking the client secret id
        public static ClientSecretInfo ParseSecretInfo(ReadOnlySpan<char> span, ref MemoryExtensions.SpanSplitEnumerator<char> parts)
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
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp)),
                description.ToString(),
                isLegacy.StartsWith(LegacySecretPrefix)
            );
        }

        public static ClientSecretInfo? FindById(string secret, int id)
        {
            var span = secret.AsSpan();
            var parts = span.SplitAny(',', '.');
            while (parts.MoveNext())
            {
                if (int.Parse(span[parts.Current]) == id)
                    return Functions.ParseSecretInfo(span, ref parts);

                parts.MoveNext(); //timestamp
                parts.MoveNext(); //key
                parts.MoveNext(); //description
            }

            return null;
        }

        public static (string, ClientSecretInfo) AddSecret(string? secrets, string obfuscatedSecret, string? desc = null)
        {
            var id = 0;

            if (secrets is not null)
            {
                var span = secrets.AsSpan();
                var parts = span.SplitAny(',', '.');
                while (parts.MoveNext())
                {
                    if (int.Parse(span[parts.Current]) >= id)
                        id = int.Parse(span[parts.Current]) + 1;

                    parts.MoveNext(); //timestamp
                    parts.MoveNext(); //key
                    parts.MoveNext(); //description
                }
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var description = desc ?? obfuscatedSecret[^6..];
            var secretInfoString = $"{id}.{timestamp}.{obfuscatedSecret}.{description}";
            var secretsString = secrets is null ? secretInfoString : $"{secrets},{secretInfoString}";
            var secretInfo = new ClientSecretInfo(id, DateTimeOffset.UtcNow, description, false);
            return (secretsString, secretInfo);
        }

        public static string? RemoveSecret(string secrets, int id)
        {
            var span = secrets.AsSpan();
            var parts = span.SplitAny(',', '.');
            while (parts.MoveNext())
            {
                if (int.Parse(span[parts.Current]) == id)
                {
                    var start = span[..parts.Current.Start.Value];
                    if (start.StartsWith(','))
                        start = start[1..];

                    if (start.EndsWith(','))
                        start = start[..^1];

                    parts.MoveNext(); //timestamp
                    parts.MoveNext(); //key
                    parts.MoveNext(); //description

                    var end = span[parts.Current.End.Value..];
                    if (end.StartsWith(','))
                        end = end[1..];

                    if (end.EndsWith(','))
                        end = end[..^1];

                    var comma = start.IsEmpty || end.IsEmpty ? string.Empty : ",";
                    var result = $"{start}{comma}{end}";
                    return result.Equals(string.Empty) ? null : result;
                }

                parts.MoveNext(); //timestamp
                parts.MoveNext(); //key
                parts.MoveNext(); //description
            }

            return secrets;
        }
        
        // TODO: Write a unit test for this.
        public static bool ValidateLegacySecret(string secret, string comparand)
        {
            var bytes = Encoding.UTF8.GetBytes(secret);
            var hash = SHA256.HashData(bytes);
            var comparandValue = Convert.FromBase64String(comparand.Remove(0, LegacySecretPrefix.Length));
            return CryptographicOperations.FixedTimeEquals(hash, comparandValue);
        }
    }
}
