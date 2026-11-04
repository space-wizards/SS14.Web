using System.Collections.Generic;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonTierMapping))]
public sealed class TestPatreonTierMapping
{
    [Test]
    public void TestWithRewardNewTier()
    {
        var (map, names) = PatreonTierMapping.WithReward(
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            "1",
            "Captain");

        Assert.Multiple(() =>
        {
            Assert.That(map, Is.EqualTo(new Dictionary<string, string> { ["1"] = "Captain" }));
            Assert.That(names, Is.EqualTo(new Dictionary<string, string> { ["Captain"] = "Captain" }));
        });
    }

    [Test]
    public void TestWithRewardKeepsOldDisplayName()
    {
        var map = new Dictionary<string, string> { ["1"] = "Assistant" };
        var names = new Dictionary<string, string> { ["Assistant"] = "Greytide" };

        var (newMap, newNames) = PatreonTierMapping.WithReward(map, names, "1", "Captain");

        Assert.Multiple(() =>
        {
            Assert.That(newMap["1"], Is.EqualTo("Captain"));
            Assert.That(newNames["Captain"], Is.EqualTo("Greytide"));
        });
    }

    [Test]
    public void TestWithRewardKeepsExistingDisplayName()
    {
        var map = new Dictionary<string, string> { ["1"] = "Assistant" };
        var names = new Dictionary<string, string> { ["Assistant"] = "Assistant", ["Captain"] = "Comdom" };

        var (_, newNames) = PatreonTierMapping.WithReward(map, names, "1", "Captain");

        Assert.That(newNames["Captain"], Is.EqualTo("Comdom"));
    }

    [Test]
    public void TestWithRewardDoesNotMutateInput()
    {
        var map = new Dictionary<string, string> { ["1"] = "Assistant" };
        var names = new Dictionary<string, string>();

        var (newMap, newNames) = PatreonTierMapping.WithReward(map, names, "1", "Captain");

        Assert.Multiple(() =>
        {
            Assert.That(newMap, Is.Not.SameAs(map));
            Assert.That(newNames, Is.Not.SameAs(names));
            Assert.That(map["1"], Is.EqualTo("Assistant"));
            Assert.That(names, Is.Empty);
        });
    }

    [Test]
    public void TestWithNewTiers()
    {
        var map = new Dictionary<string, string> { ["1"] = "Assistant" };
        var names = new Dictionary<string, string> { ["Assistant"] = "Greytide" };
        var tiers = new Dictionary<string, string> { ["1"] = "Renamed", ["2"] = "Captain" };

        var (newMap, newNames, added) = PatreonTierMapping.WithNewTiers(map, names, tiers);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.EqualTo(1));
            Assert.That(newMap["1"], Is.EqualTo("Assistant"));
            Assert.That(newMap["2"], Is.EqualTo("Captain"));
            Assert.That(newNames["Assistant"], Is.EqualTo("Greytide"));
            Assert.That(newNames["Captain"], Is.EqualTo("Captain"));
            Assert.That(map, Has.Count.EqualTo(1));
        });
    }
}
