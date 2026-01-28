using System.Collections.Generic;
using OpenIddict.Abstractions;

namespace SS14.Web.OpenId;

public static class OpenIdConstants
{
    public const string EncryptionAlgorithmSetting = "space:EncryptionAlgorithm";
    public const string SigningAlgorithmSetting = "space:SigningAlgorithm";
    public const string DisabledSetting = "space:Disabled";
    public const string AllowPlainPkce = "space:AllowPlainPkce";
}
