using System.Text.Json;
using NUnit.Framework;

namespace SS14.Web.Tests
{
    public class TestPatreonConnection
    {
        [Test]
        public void TestParseTiers()
        {
            const string json = @"
{
  ""data"": {
    ""attributes"": {},
    ""id"": ""1"",
    ""relationships"": {
      ""memberships"": {
        ""data"": [
          {
            ""id"": ""6f12afba-4432-4e53-b2e7-721486e66bf3"",
            ""type"": ""member""
          }
        ]
      }
    },
    ""type"": ""user""
  },
  ""included"": [
    {
      ""attributes"": {},
      ""id"": ""6f12afba-4432-4e53-b2e7-721486e66bf3"",
      ""relationships"": {
        ""currently_entitled_tiers"": {
          ""data"": [
            {
              ""id"": ""2"",
              ""type"": ""tier""
            }
          ]
        }
      },
      ""type"": ""member""
    },
    {
      ""attributes"": {},
      ""id"": ""2"",
      ""type"": ""tier""
    }
  ]
}";
            using var doc = JsonDocument.Parse(json);
            var tiers = PatreonConnectionHandler.ParseTiers(doc.RootElement);

            Assert.That(tiers, Is.EquivalentTo(new[] {"2"}));
        }
    }
}