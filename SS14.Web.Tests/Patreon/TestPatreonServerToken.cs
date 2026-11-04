using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonServerToken))]
public sealed class TestPatreonServerToken
{
    [Test]
    public void TestGenerateIsUrlSafeAndUnique()
    {
        var a = PatreonServerToken.Generate();
        var b = PatreonServerToken.Generate();

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a, Does.Match("^[A-Za-z0-9_-]+$"));
        });
    }

    [Test]
    public void TestHash()
    {
        var hash = PatreonServerToken.Hash("token");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Has.Length.EqualTo(64));
            Assert.That(PatreonServerToken.Hash("token"), Is.EqualTo(hash));
            Assert.That(PatreonServerToken.Hash("Token"), Is.Not.EqualTo(hash));
        });
    }
}
