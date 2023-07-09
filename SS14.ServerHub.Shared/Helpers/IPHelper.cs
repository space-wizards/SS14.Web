using System.Net;
using System.Net.Sockets;

namespace SS14.ServerHub.Shared.Helpers;

/// <summary>
/// Helper functions for working with IP addresses and CIDR-notation ranges.
/// </summary>
public static class IPHelper
{
    public static bool TryParseIpOrCidr(string str, out (IPAddress, int) cidr)
    {
        if (IPAddress.TryParse(str, out var addr))
        {
            cidr = (addr, addr.AddressFamily switch
            {
                AddressFamily.InterNetwork => 32,
                AddressFamily.InterNetworkV6 => 128,
                _ => throw new ArgumentException(nameof(str))
            });
            return true;
        }

        return TryParseCidr(str, out cidr);
    }

    public static bool TryParseCidr(string str, out (IPAddress, int) cidr)
    {
        cidr = default;

        var split = str.Split("/");
        if (split.Length != 2)
            return false;

        if (!IPAddress.TryParse(split[0], out cidr.Item1!))
            return false;

        if (!int.TryParse(split[1], out cidr.Item2))
            return false;

        return true;
    }

    public static string FormatCidr(this (IPAddress, int) cidr)
    {
        var (addr, range) = cidr;

        return $"{addr}/{range}";
    }
}