using System.ComponentModel.DataAnnotations;

namespace WorkControl.Api.Contracts;

public sealed record RegisterDeviceRequest(
    [property: Required, MaxLength(128)] string AgentId,
    [property: Required, MaxLength(128)] string ComputerName,
    Guid? EmployeeId,
    [property: MaxLength(128)] string? EmployeeName = null);

public sealed record RegisterDeviceResponse(Guid DeviceId, DateTimeOffset RegisteredAtUtc);

public sealed record AgentEventRequest(
    Guid EventId,
    [property: Required, MaxLength(128)] string AgentId,
    [property: Required, MaxLength(64)] string EventType,
    DateTimeOffset OccurredAtUtc,
    string? PayloadJson);

public sealed record AgentEventResponse(Guid EventId, bool Duplicate, DateTimeOffset ReceivedAtUtc);

public sealed record DashboardRow(
    string Employee,
    string Group,
    string Computer,
    DateTimeOffset? LastSeenAtUtc,
    string Status,
    long ObservedSeconds,
    long ActiveSeconds,
    string ActivityState);
