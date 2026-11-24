using System.Text.Json;
using WorkControl.Api.Domain;

namespace WorkControl.Api.Modules.Telemetry;

public sealed record ActivitySummary(long ObservedSeconds, long ActiveSeconds);

public static class ActivityMetrics
{
    public static string GetActivityState(IEnumerable<AgentEvent> events)
    {
        var latest = events
            .Where(x => x.EventType == "activity.status")
            .OrderByDescending(x => x.ReceivedAtUtc)
            .FirstOrDefault();
        if (!TryReadPayload(latest?.PayloadJson, out var state, out var longAbsence)) return "Нет данных";

        return state switch
        {
            "active" => "Активен",
            "idle" when longAbsence => "Длительное отсутствие",
            "idle" => "Неактивен",
            _ => "Нет данных"
        };
    }

    public static ActivitySummary GetActivitySummary(
        IEnumerable<AgentEvent> events,
        DateTimeOffset startOfDay,
        DateTimeOffset now,
        int maximumSignalGapSeconds)
    {
        var activityEvents = events
            .Where(x => x.EventType == "activity.status")
            .OrderBy(x => x.OccurredAtUtc)
            .ToList();
        if (activityEvents.Count == 0) return new ActivitySummary(0, 0);

        long observedSeconds = 0;
        long activeSeconds = 0;
        for (var index = 0; index < activityEvents.Count; index++)
        {
            var current = activityEvents[index];
            var intervalStart = current.OccurredAtUtc < startOfDay ? startOfDay : current.OccurredAtUtc;
            var nextAt = index + 1 < activityEvents.Count ? activityEvents[index + 1].OccurredAtUtc : now;
            var intervalEnd = nextAt < now ? nextAt : now;
            var maximumEnd = current.OccurredAtUtc.AddSeconds(maximumSignalGapSeconds);
            if (intervalEnd > maximumEnd) intervalEnd = maximumEnd;
            if (intervalEnd <= intervalStart) continue;

            var seconds = (long)(intervalEnd - intervalStart).TotalSeconds;
            observedSeconds += seconds;
            if (TryReadPayload(current.PayloadJson, out var state, out _) && state == "active")
            {
                activeSeconds += seconds;
            }
        }

        return new ActivitySummary(observedSeconds, activeSeconds);
    }

    private static bool TryReadPayload(string? payloadJson, out string? state, out bool longAbsence)
    {
        state = null;
        longAbsence = false;
        if (string.IsNullOrWhiteSpace(payloadJson)) return false;

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty("state", out var stateElement)) return false;
            state = stateElement.GetString();
            if (document.RootElement.TryGetProperty("longAbsence", out var longAbsenceElement) &&
                longAbsenceElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                longAbsence = longAbsenceElement.GetBoolean();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
