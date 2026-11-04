using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonSecretProtector))]
public sealed class TestPatreonSecretProtector
{
    [Test]
    public void TestRoundTrip()
    {
        var protector = new PatreonSecretProtector(new EphemeralDataProtectionProvider());

        var protectedText = protector.Protect("secret");

        Assert.Multiple(() =>
        {
            Assert.That(protectedText, Is.Not.EqualTo("secret"));
            Assert.That(protector.Unprotect(protectedText), Is.EqualTo("secret"));
        });
    }
}
