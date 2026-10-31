using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SS14.Auth.Shared.Config;
using SS14.Auth.Shared.Patreon;

namespace SS14.Web.Tests.Patreon;

[TestFixture, TestOf(typeof(PatreonApiClient))]
public sealed class TestPatreonApiClient
{
    private const string EmptyPage = """{ "data": [] }""";

    private static PatreonApiClient MakeClient(FakeHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://patreon.test/") };
        var cfg = Options.Create(new PatreonConfiguration { ClientId = "id", ClientSecret = "secret" });
        return new PatreonApiClient(http, cfg);
    }

    [Test]
    public async Task TestGetMembersPageFirstPage()
    {
        var handler = new FakeHandler(() => Json(EmptyPage));
        var client = MakeClient(handler);

        var page = await client.GetMembersPageAsync("token", "c1", null);

        var request = handler.Requests[0];
        Assert.Multiple(() =>
        {
            Assert.That(page.Members, Is.Empty);
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/oauth2/v2/campaigns/c1/members"));
            Assert.That(request.RequestUri.Query, Does.Not.Contain("cursor"));
            Assert.That(request.Headers.Authorization!.ToString(), Is.EqualTo("Bearer token"));
        });
    }

    [Test]
    public async Task TestGetMembersPageSendsCursor()
    {
        var handler = new FakeHandler(() => Json(EmptyPage));
        var client = MakeClient(handler);

        await client.GetMembersPageAsync("token", "c1", "a/b");

        Assert.That(handler.Requests[0].RequestUri!.Query, Does.Contain("page%5Bcursor%5D=a%2Fb"));
    }

    [Test]
    public async Task TestRetriesOnTooManyRequests()
    {
        var handler = new FakeHandler(RateLimited, RateLimited, () => Json(EmptyPage));
        var client = MakeClient(handler);

        await client.GetMembersPageAsync("token", "c1", null);

        Assert.That(handler.Requests, Has.Count.EqualTo(3));
    }

    [Test]
    public void TestGivesUpAfterMaxAttempts()
    {
        var handler = new FakeHandler(RateLimited);
        var client = MakeClient(handler);

        Assert.That(
            async () => await client.GetMembersPageAsync("token", "c1", null),
            Throws.InstanceOf<HttpRequestException>());
        Assert.That(handler.Requests, Has.Count.EqualTo(5));
    }

    [Test]
    public void TestDoesNotRetryOtherErrors()
    {
        var handler = new FakeHandler(() => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = MakeClient(handler);

        Assert.That(
            async () => await client.GetMembersPageAsync("token", "c1", null),
            Throws.InstanceOf<HttpRequestException>());
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task TestRefreshToken()
    {
        var handler = new FakeHandler(() => Json("""
            { "access_token": "new-access", "refresh_token": "new-refresh", "expires_in": 3600 }
            """));
        var client = MakeClient(handler);

        var tokens = await client.RefreshTokenAsync("old-refresh");

        Assert.Multiple(() =>
        {
            Assert.That(tokens.AccessToken, Is.EqualTo("new-access"));
            Assert.That(tokens.RefreshToken, Is.EqualTo("new-refresh"));
            Assert.That(tokens.ExpiresAt, Is.EqualTo(DateTimeOffset.UtcNow.AddHours(1)).Within(TimeSpan.FromMinutes(1)));
            Assert.That(handler.Bodies[0], Does.Contain("refresh_token=old-refresh"));
        });
    }

    [Test]
    public async Task TestCreateWebhookReturnsSecret()
    {
        var handler = new FakeHandler(() => Json("""
            { "data": { "attributes": { "secret": "hook-secret" } } }
            """));
        var client = MakeClient(handler);

        var secret = await client.CreateWebhookAsync("token", "c1", "https://example.test/hook");

        Assert.Multiple(() =>
        {
            Assert.That(secret, Is.EqualTo("hook-secret"));
            Assert.That(handler.Bodies[0], Does.Contain("https://example.test/hook"));
            Assert.That(handler.Bodies[0], Does.Contain("\"id\":\"c1\""));
        });
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private static HttpResponseMessage RateLimited()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
        return response;
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage>[] _responses;

        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> Bodies { get; } = new();

        public FakeHandler(params Func<HttpResponseMessage>[] responses)
        {
            _responses = responses;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responses[Math.Min(Requests.Count - 1, _responses.Length - 1)]();
        }
    }
}
