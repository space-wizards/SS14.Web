using System.Collections.Generic;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonMembershipManager))]
public sealed class TestPatreonMembershipManager
{
    private static readonly Dictionary<string, string> TierMap = new()
    {
        ["1"] = "Assistant",
        ["2"] = "Captain"
    };

    [Test]
    public void TestMapTier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatreonMembershipManager.MapTier(TierMap, ["2"]), Is.EqualTo("Captain"));
            Assert.That(PatreonMembershipManager.MapTier(TierMap, ["unknown"]), Is.Null);
            Assert.That(PatreonMembershipManager.MapTier(TierMap, []), Is.Null);
        });
    }

    [Test]
    public void TestMapTierSkipsDisabledTiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatreonMembershipManager.MapTier(TierMap, ["2"], ["2"]), Is.Null);
            Assert.That(PatreonMembershipManager.MapTier(TierMap, ["2", "1"], ["2"]), Is.EqualTo("Assistant"));
            Assert.That(PatreonMembershipManager.MapTier(TierMap, ["2"], ["1"]), Is.EqualTo("Captain"));
        });
    }
}
