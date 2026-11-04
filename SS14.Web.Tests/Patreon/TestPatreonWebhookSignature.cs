using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonWebhookSignature))]
public sealed class TestPatreonWebhookSignature
{
    private const string Secret = "4";
    private static readonly byte[] Body = """{"data": {}}"""u8.ToArray();

    private static string Sign(byte[] body, string secret)
    {
        return Convert.ToHexString(HMACMD5.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
    }

    [Test]
    public void TestValid()
    {
        Assert.That(PatreonWebhookSignature.Verify(Body, Secret, Sign(Body, Secret)), Is.True);
    }

    [Test]
    public void TestWrongSecret()
    {
        Assert.That(PatreonWebhookSignature.Verify(Body, Secret, Sign(Body, "67")), Is.False);
    }

    [Test]
    public void TestTamperedBody()
    {
        var signature = Sign(Body, Secret);
        var tampered = """{"data": {"x": 1}}"""u8.ToArray();

        Assert.That(PatreonWebhookSignature.Verify(tampered, Secret, signature), Is.False);
    }

    [Test]
    public void TestMalformedSignature()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatreonWebhookSignature.Verify(Body, Secret, null), Is.False);
            Assert.That(PatreonWebhookSignature.Verify(Body, Secret, ""), Is.False);
            Assert.That(PatreonWebhookSignature.Verify(Body, Secret, "abc"), Is.False);
            Assert.That(PatreonWebhookSignature.Verify(Body, Secret, "zzzz"), Is.False);
        });
    }
}
