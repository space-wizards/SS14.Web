using System.Net;
using System.Net.Sockets;
using NpgsqlTypes;

namespace SS14.ServerHub.Shared.Helpers;

/// <summary>
/// Helper functions for working with IP addresses and CIDR-notation ranges.
/// </summary>
public static class IPHelper
{
    public static bool TryParseIpOrCidr(string str, out NpgsqlCidr cidr)
    {
        if (IPAddress.TryParse(str, out var addr))
        {
            cidr = new NpgsqlCidr(addr, addr.AddressFamily switch
            {
                AddressFamily.InterNetwork => 32,
                AddressFamily.InterNetworkV6 => 128,
                _ => throw new ArgumentException(null, nameof(str)),
            });
            return true;
        }

        return TryParseCidr(str, out cidr);
    }

    public static bool TryParseCidr(string str, out NpgsqlCidr cidr)
    {
        cidr = default;

        var split = str.Split("/");
        if (split.Length != 2)
            return false;

        if (!IPAddress.TryParse(split[0], out var address))
            return false;

        if (!byte.TryParse(split[1], out var mask))
            return false;

        cidr = new NpgsqlCidr(address, mask);

        return true;
    }

    public static string FormatCidr(this NpgsqlCidr cidr)
    {
        var (addr, range) = cidr;

        return $"{addr}/{range}";
    }
}
