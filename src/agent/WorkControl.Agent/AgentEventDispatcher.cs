using System.Net.Http.Json;

namespace WorkControl.Agent;

public sealed record AgentControlEvent(Guid EventId, string AgentId, string EventType, DateTimeOffset OccurredAtUtc, string PayloadJson);
public sealed record AgentCredential(Guid KeyId, string Secret);
public sealed record AgentEnrollmentRequest(string Code, string ComputerName);
public sealed record AgentEnrollmentResponse(Guid DeviceId, string AgentId, Guid KeyId, string Secret, string Status);
public sealed record RotatedAgentCredential(Guid KeyId, string Secret, DateTimeOffset ExpiresAtUtc);

public sealed class AgentEventDispatcher(HttpClient httpClient, LocalEventStore store)
{
    public async Task QueueAndSendAsync(
        string apiUrl,
        AgentCredential credential,
        AgentControlEvent activityEvent,
        CancellationToken cancellationToken = default)
    {
        store.Enqueue(activityEvent);
        await FlushAsync(apiUrl, credential, cancellationToken);
    }

    public async Task<AgentEnrollmentResponse> EnrollAsync(string apiUrl, string code, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"{apiUrl.TrimEnd('/')}/api/agent/v1/enroll",
            new AgentEnrollmentRequest(code, Environment.MachineName), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentEnrollmentResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("API не вернул учётные данные агента.");
    }

    public async Task FlushAsync(
        string apiUrl,
        AgentCredential credential,
        CancellationToken cancellationToken = default)
    {
        var normalizedApiUrl = apiUrl.TrimEnd('/');
        foreach (var pending in store.GetPending())
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{normalizedApiUrl}/api/agent/v1/events")
            {
                Content = JsonContent.Create(pending)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "WorkControl", $"{credential.KeyId}.{credential.Secret}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            store.Remove(pending.EventId);
        }
    }

    public async Task<RotatedAgentCredential> RotateCredentialAsync(string apiUrl, AgentCredential credential, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl.TrimEnd('/')}/api/agent/v1/credentials/rotate");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("WorkControl", $"{credential.KeyId}.{credential.Secret}");
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RotatedAgentCredential>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("API не вернул новые учётные данные агента.");
    }
}
