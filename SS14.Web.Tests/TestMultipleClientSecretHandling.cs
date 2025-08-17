#nullable enable
using System;
using JetBrains.Annotations;
using NUnit.Framework;
using SS14.Web.Models.Types;
using SS14.Web.OpenId.Services;

namespace SS14.Web.Tests;

public class TestMultipleClientSecretHandling
{
    [TestCase( "0.1755342104.key1111.desc", 0, ExpectedResult = 0)]
    [TestCase( "0.1755342104.key1111.desc", 1, ExpectedResult = null)]
    [TestCase( "0.1755342104.key1111.desc,1.1755342104.key2222.desc", 1, ExpectedResult = 1)]
    public int? TestFindById(string secrets, int id)
    {
        return SpaceApplicationManager.Functions.FindById(secrets, id)?.Id;
    }

    [TestCase( "0.1755342104.key1111.desc", "key2222", 2, 1)]
    [TestCase( null, "key2222", 1, 0)]
    [TestCase( "1.1755342104.key1111.desc", "key2222", 2, 2)]
    public void TestAdd(string? secrets, string obfuscatedSecret, int count, int id)
    {
        var result = SpaceApplicationManager.Functions.AddSecret(secrets, obfuscatedSecret);
        // 3 dots for each secret.
        Assert.That(() => result.Item1.AsSpan().Count('.'), Is.EqualTo(count * 3));
        Assert.That(() => result.Item2.Id, Is.EqualTo(id));
        Assert.That(() => result.Item2.Description, Is.EqualTo(obfuscatedSecret[^6..]));
    }

    [TestCase( "0.1755342104.key1111.desc", 0, ExpectedResult = null)]
    [TestCase( "0.1755342104.key1111.desc", 1, ExpectedResult = "0.1755342104.key1111.desc")]
    [TestCase( "0.1755342104.key1111.desc,1.1755342104.key2222.desc", 1,
        ExpectedResult = "0.1755342104.key1111.desc")]
    [TestCase( "0.1755342104.key1111.desc,1.1755342104.key2222.desc", 0,
        ExpectedResult = "1.1755342104.key2222.desc")]
    [TestCase( "0.1755342104.key1111.desc,1.1755342104.key2222.desc,2.1755342104.key3333.desc", 1,
        ExpectedResult = "0.1755342104.key1111.desc,2.1755342104.key3333.desc")]
    public string? TestRemove(string secrets, int id)
    {
        return SpaceApplicationManager.Functions.RemoveSecret(secrets, id);
    }
}
