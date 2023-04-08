using System.Text.Json;
using NUnit.Framework;
using SS14.ServerHub.Utility;

namespace SS14.Web.Tests;

[TestFixture, TestOf(typeof(JsonHelper))]
public sealed class TestJsonHelper
{
    [Test]
    public static void TestValidateJson()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => JsonHelper.CheckJsonValid("""{"asdf": 5}"""u8), Throws.Nothing);
            Assert.That(() => JsonHelper.CheckJsonValid(""" "asdf" """u8), Throws.Nothing);
            Assert.That(() => JsonHelper.CheckJsonValid("""{"asdf": 5"""u8), Throws.InstanceOf<JsonException>());
            Assert.That(() => JsonHelper.CheckJsonValid(""" ["""u8), Throws.InstanceOf<JsonException>());
            Assert.That(() => JsonHelper.CheckJsonValid(""" " """u8), Throws.InstanceOf<JsonException>());
            Assert.That(() => JsonHelper.CheckJsonValid(""" " """u8), Throws.InstanceOf<JsonException>());
        });
    }
}