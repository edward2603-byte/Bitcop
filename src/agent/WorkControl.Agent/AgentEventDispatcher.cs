using System.Net.Http.Json;

namespace WorkControl.Agent;

public sealed record DeviceRegistration(string AgentId, string ComputerName, Guid? EmployeeId, string EmployeeName);
public sealed record AgentControlEvent(Guid EventId, string AgentId, string EventType, DateTimeOffset OccurredAtUtc, string PayloadJson);

public sealed class AgentEventDispatcher(HttpClient httpClient, LocalEventStore store)
{
    public async Task QueueAndSendAsync(
        string apiUrl,
        DeviceRegistration registration,
        AgentControlEvent activityEvent,
        CancellationToken cancellationToken = default)
    {
        store.Enqueue(activityEvent);
        await RegisterAndFlushAsync(apiUrl, registration, cancellationToken);
    }

    public async Task RegisterAndFlushAsync(
        string apiUrl,
        DeviceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var normalizedApiUrl = apiUrl.TrimEnd('/');
        var registered = await httpClient.PostAsJsonAsync(
            $"{normalizedApiUrl}/api/devices/register",
            registration,
            cancellationToken);
        registered.EnsureSuccessStatusCode();

        foreach (var pending in store.GetPending())
        {
            var response = await httpClient.PostAsJsonAsync(
                $"{normalizedApiUrl}/api/telemetry/events",
                pending,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            store.Remove(pending.EventId);
        }
    }
}
