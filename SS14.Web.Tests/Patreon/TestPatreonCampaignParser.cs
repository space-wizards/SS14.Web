using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonCampaignParser))]
public sealed class TestPatreonCampaignParser
{
    private static JsonElement Parse(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    [Test]
    public void TestParseCampaigns()
    {
        var campaigns = PatreonCampaignParser.ParseCampaigns(Parse("""
            {
              "data": [
                {
                  "id": "c1",
                  "attributes": { "vanity": "vanity" },
                  "relationships": {
                    "creator": { "data": { "id": "u1", "type": "user" } },
                    "tiers": { "data": [ { "id": "t1" }, { "id": "unknown" } ] }
                  }
                }
              ],
              "included": [
                { "type": "tier", "id": "t1", "attributes": { "title": "Janitor" } },
                { "type": "tier", "id": "t2", "attributes": { "title": "Unlisted" } },
                { "type": "user", "id": "u1", "attributes": { "full_name": "Joe Genero" } }
              ]
            }
            """));

        Assert.That(campaigns, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(campaigns[0].Id, Is.EqualTo("c1"));
            Assert.That(campaigns[0].Name, Is.EqualTo("Joe Genero"));
            Assert.That(campaigns[0].Tiers, Is.EqualTo(new Dictionary<string, string> { ["t1"] = "Janitor" }));
        });
    }

    [Test]
    public void TestNameFallsBackToVanity()
    {
        var campaigns = PatreonCampaignParser.ParseCampaigns(Parse("""
            { "data": [ { "id": "c1", "attributes": { "vanity": "vanity" } } ] }
            """));

        Assert.That(campaigns[0].Name, Is.EqualTo("vanity"));
    }

    [Test]
    public void TestNameFallsBackToId()
    {
        var campaigns = PatreonCampaignParser.ParseCampaigns(Parse("""
            { "data": [ { "id": "c1", "attributes": { "vanity": null } } ] }
            """));

        Assert.Multiple(() =>
        {
            Assert.That(campaigns[0].Name, Is.EqualTo(PatreonCampaignParser.FallbackName("c1")));
            Assert.That(campaigns[0].Tiers, Is.Empty);
        });
    }

    [Test]
    public void TestEmptyAndMalformed()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatreonCampaignParser.ParseCampaigns(Parse("{}")), Is.Empty);
            Assert.That(PatreonCampaignParser.ParseCampaigns(Parse("""{ "data": [ {} ] }""")), Is.Empty);
        });
    }
}
