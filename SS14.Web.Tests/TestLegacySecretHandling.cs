using NUnit.Framework;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Tests;

public class TestLegacySecretHandling
{
    // Test Hash generated using the old version of SS14.Web:
    private const string DummyHash = "nodXBUVT5CFsKk+1JJivokaRZIOS1jDsN5QD2+iLkGc=";

    [TestCase( "2tXYv/rXpB91juHz13w2ib8zODPPmHQfXhCr5kuCLi8pKuTF", ExpectedResult = true)]
    [TestCase( "2tXYv/rXpB91juHz13w2ib8zODPPmHQfXhCr5kuCLi8pKuTE", ExpectedResult = false)]
    public bool TestValidation(string secret)
    {
        return SpaceApplicationManager.Functions.ValidateLegacySecret(secret, DummyHash);
    }
}
