using System.Net;
using Microsoft.Data.Sqlite;
using WorkControl.Agent;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class AgentBehaviorTests
{
    [Theory]
    [InlineData(59, "active", false)]
    [InlineData(60, "idle", false)]
    [InlineData(299, "idle", false)]
    [InlineData(300, "idle", true)]
    public void Activity_thresholds_are_independent(long idleSeconds, string expectedState, bool expectedLongAbsence)
    {
        var snapshot = UserActivityProbe.Classify(idleSeconds, new ActivityOptions(60, 300));
        Assert.Equal(expectedState, snapshot.State);
        Assert.Equal(idleSeconds, snapshot.IdleSeconds);
        Assert.Equal(expectedLongAbsence, snapshot.IsLongAbsence);
    }

    [Theory]
    [InlineData("http://localhost:5080")]
    [InlineData("http://127.0.0.1:5080")]
    public void Local_agent_accepts_only_loopback_addresses(string apiUrl)
    {
        AgentSettings.EnsureLocalApiUrl(apiUrl);
        Assert.Throws<InvalidOperationException>(() => AgentSettings.EnsureLocalApiUrl("http://192.168.1.20:5080"));
    }

    [Fact]
    public async Task Offline_event_stays_in_sqlite_and_is_sent_after_connection_recovers()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"workcontrol-agent-{Guid.NewGuid():N}.db");
        try
        {
            var store = new LocalEventStore(databasePath);
            store.Initialize();
            var activityEvent = new AgentControlEvent(
                Guid.NewGuid(),
                "offline-agent",
                "activity.status",
                DateTimeOffset.UtcNow,
                "{\"state\":\"idle\",\"idleSeconds\":61,\"longAbsence\":false}");
            var registration = new DeviceRegistration("offline-agent", "OFFLINE-PC", null, "Тест очереди");
            var offlineDispatcher = new AgentEventDispatcher(
                new HttpClient(new StubHttpHandler(_ => throw new HttpRequestException("Сеть недоступна"))),
                store);

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                offlineDispatcher.QueueAndSendAsync("http://127.0.0.1:5080", registration, activityEvent));
            Assert.Equal(activityEvent.EventId, Assert.Single(store.GetPending()).EventId);

            var restoredStore = new LocalEventStore(databasePath);
            restoredStore.Initialize();
            var requests = new List<string>();
            var onlineDispatcher = new AgentEventDispatcher(
                new HttpClient(new StubHttpHandler(request =>
                {
                    requests.Add(request.RequestUri!.AbsolutePath);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                })),
                restoredStore);
            await onlineDispatcher.RegisterAndFlushAsync("http://127.0.0.1:5080", registration);

            Assert.Empty(restoredStore.GetPending());
            Assert.Equal(new[] { "/api/devices/register", "/api/telemetry/events" }, requests);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
