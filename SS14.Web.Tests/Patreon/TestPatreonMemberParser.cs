using System.Text.Json;
using NUnit.Framework;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonMemberParser))]
public sealed class TestPatreonMemberParser
{
    private const string MemberJson = """
        {
          "id": "m1",
          "type": "member",
          "attributes": { "patron_status": "active_patron" },
          "relationships": {
            "user": { "data": { "id": "u1", "type": "user" } },
            "currently_entitled_tiers": { "data": [ { "id": "t1", "type": "tier" }, { "id": "t2", "type": "tier" } ] }
          }
        }
        """;

    private static JsonElement Parse(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    [Test]
    public void TestParseMember()
    {
        var member = PatreonMemberParser.ParseMember(Parse(MemberJson));

        Assert.That(member, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(member!.MemberId, Is.EqualTo("m1"));
            Assert.That(member.UserId, Is.EqualTo("u1"));
            Assert.That(member.PatronStatus, Is.EqualTo("active_patron"));
            Assert.That(member.TierIds, Is.EqualTo(new[] { "t1", "t2" }));
        });
    }

    [Test]
    public void TestParseMemberWithoutOptionalFields()
    {
        var member = PatreonMemberParser.ParseMember(Parse("""
            { "id": "m1", "relationships": { "user": { "data": { "id": "u1" } } } }
            """));

        Assert.That(member, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(member!.PatronStatus, Is.Null);
            Assert.That(member.TierIds, Is.Empty);
        });
    }

    [Test]
    public void TestParseMemberWithoutUser()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatreonMemberParser.ParseMember(Parse("""
                { "id": "m1", "relationships": { "user": { "data": null } } }
                """)), Is.Null);
            Assert.That(PatreonMemberParser.ParseMember(Parse("""{ "id": "m1" }""")), Is.Null);
            Assert.That(PatreonMemberParser.ParseMember(Parse("""{ "relationships": {} }""")), Is.Null);
            Assert.That(PatreonMemberParser.ParseMember(Parse("[]")), Is.Null);
        });
    }

    [Test]
    public void TestParseWebhook()
    {
        var member = PatreonMemberParser.ParseWebhook(Parse("{ \"data\": " + MemberJson + " }"));

        Assert.Multiple(() =>
        {
            Assert.That(member?.MemberId, Is.EqualTo("m1"));
            Assert.That(PatreonMemberParser.ParseWebhook(Parse("{}")), Is.Null);
        });
    }

    [Test]
    public void TestParsePage()
    {
        var page = PatreonMemberParser.ParsePage(Parse("""
            {
              "data": [
                {
                  "id": "m1",
                  "relationships": { "user": { "data": { "id": "u1" } } }
                },
                { "id": "orphan", "relationships": {} }
              ],
              "meta": { "pagination": { "cursors": { "next": "abc" } } }
            }
            """));

        Assert.Multiple(() =>
        {
            Assert.That(page.Members, Has.Count.EqualTo(1));
            Assert.That(page.NextCursor, Is.EqualTo("abc"));
        });
    }

    [Test]
    public void TestParsePageLastPage()
    {
        var page = PatreonMemberParser.ParsePage(Parse("""
            { "data": [], "meta": { "pagination": { "cursors": { "next": null } } } }
            """));

        Assert.Multiple(() =>
        {
            Assert.That(page.Members, Is.Empty);
            Assert.That(page.NextCursor, Is.Null);
        });
    }
}
