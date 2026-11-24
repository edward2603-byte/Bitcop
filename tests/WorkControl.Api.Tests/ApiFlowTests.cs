using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkControl.Api.Contracts;
using WorkControl.Api.Infrastructure;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class ApiFlowTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"workcontrol-api-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:WorkControl"] = $"Data Source={_databasePath}",
                    ["Database:Provider"] = "Sqlite",
                    ["Database:AutoMigrate"] = "true",
                    ["Presence:OnlineThresholdSeconds"] = "90"
                }));
        });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Device_event_duplicate_persistence_and_dashboard_form_one_working_chain()
    {
        var client = _client!;
        var registration = new RegisterDeviceRequest("integration-agent", "INTEGRATION-PC", null, "Интеграционный тест");
        var registrationResponse = await client.PostAsJsonAsync("/api/devices/register", registration);
        registrationResponse.EnsureSuccessStatusCode();

        var eventId = Guid.NewGuid();
        var activityEvent = new AgentEventRequest(
            eventId,
            registration.AgentId,
            "activity.status",
            DateTimeOffset.UtcNow,
            "{\"state\":\"idle\",\"idleSeconds\":65,\"longAbsence\":false}");

        var firstResponse = await client.PostAsJsonAsync("/api/telemetry/events", activityEvent);
        firstResponse.EnsureSuccessStatusCode();
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<AgentEventResponse>();
        Assert.NotNull(firstResult);
        Assert.False(firstResult.Duplicate);

        var duplicateResponse = await client.PostAsJsonAsync("/api/telemetry/events", activityEvent);
        duplicateResponse.EnsureSuccessStatusCode();
        var duplicateResult = await duplicateResponse.Content.ReadFromJsonAsync<AgentEventResponse>();
        Assert.NotNull(duplicateResult);
        Assert.True(duplicateResult.Duplicate);

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
            Assert.Equal(1, await db.AgentEvents.CountAsync(x => x.EventId == eventId));
            var stored = await db.AgentEvents.SingleAsync(x => x.EventId == eventId);
            Assert.Contains("\"idleSeconds\":65", stored.PayloadJson);
        }

        var dashboard = await client.GetFromJsonAsync<List<DashboardRow>>("/api/dashboard");
        var row = Assert.Single(dashboard!, x => x.Computer == registration.ComputerName);
        Assert.Equal("На связи", row.Status);
        Assert.Equal("Неактивен", row.ActivityState);
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        return Task.CompletedTask;
    }
}
