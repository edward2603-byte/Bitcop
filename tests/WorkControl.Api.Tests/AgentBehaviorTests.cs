using System.Net;
using Microsoft.Data.Sqlite;
using WorkControl.Agent;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class AgentBehaviorTests
{
    [Theory]
    [InlineData(59, "active", false)] [InlineData(60, "idle", false)]
    [InlineData(299, "idle", false)] [InlineData(300, "idle", true)]
    public void Activity_thresholds_are_independent(long idleSeconds, string expectedState, bool expectedLongAbsence)
    {
        var snapshot = UserActivityProbe.Classify(idleSeconds, new ActivityOptions(60, 300));
        Assert.Equal(expectedState, snapshot.State); Assert.Equal(idleSeconds, snapshot.IdleSeconds); Assert.Equal(expectedLongAbsence, snapshot.IsLongAbsence);
    }

    [Theory]
    [InlineData("http://localhost:5080")] [InlineData("http://127.0.0.1:5080")] [InlineData("https://workcontrol.pilot.local")]
    public void Agent_accepts_loopback_http_or_https(string apiUrl) => AgentSettings.EnsureApiUrl(apiUrl);

    [Fact]
    public void Agent_rejects_cleartext_network_address() =>
        Assert.Throws<InvalidOperationException>(() => AgentSettings.EnsureApiUrl("http://192.168.1.20:5080"));

    [Fact]
    public async Task Two_hour_offline_queue_survives_restart_and_flushes_with_credential()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"workcontrol-agent-{Guid.NewGuid():N}.db");
        try
        {
            var store = new LocalEventStore(databasePath); store.Initialize();
            var activityEvent = new AgentControlEvent(Guid.NewGuid(), "offline-agent", "activity.status", DateTimeOffset.UtcNow.AddHours(-2), "{\"state\":\"idle\"}");
            var credential = new AgentCredential(Guid.NewGuid(), "test-secret");
            var offline = new AgentEventDispatcher(new HttpClient(new StubHttpHandler(_ => throw new HttpRequestException("Сеть недоступна"))), store);
            await Assert.ThrowsAsync<HttpRequestException>(() => offline.QueueAndSendAsync("http://127.0.0.1:5080", credential, activityEvent));
            Assert.Equal(activityEvent.EventId, Assert.Single(store.GetPending()).EventId);

            var restored = new LocalEventStore(databasePath); restored.Initialize();
            var requests = new List<HttpRequestMessage>();
            var online = new AgentEventDispatcher(new HttpClient(new StubHttpHandler(request => { requests.Add(request); return new HttpResponseMessage(HttpStatusCode.OK); })), restored);
            await online.FlushAsync("http://127.0.0.1:5080", credential);
            Assert.Empty(restored.GetPending());
            var sent = Assert.Single(requests);
            Assert.Equal("/api/agent/v1/events", sent.RequestUri!.AbsolutePath);
            Assert.Equal("WorkControl", sent.Headers.Authorization!.Scheme);
            Assert.Equal($"{credential.KeyId}.{credential.Secret}", sent.Headers.Authorization.Parameter);
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(databasePath)) File.Delete(databasePath); }
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
