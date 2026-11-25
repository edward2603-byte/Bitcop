using System.ComponentModel.DataAnnotations;

namespace WorkControl.Api.Contracts;

public sealed record AgentEventRequest(
    Guid EventId,
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

public sealed record LoginRequest([property: Required, EmailAddress, MaxLength(256)] string Email, [property: Required, MaxLength(256)] string Password);
public sealed record CurrentUserResponse(Guid Id, string Email, IReadOnlyList<string> Roles);
public sealed record CreateEnrollmentResponse(string Code, DateTimeOffset ExpiresAtUtc);
public sealed record EnrollAgentRequest([property: Required, MaxLength(256)] string Code, [property: Required, MaxLength(128)] string ComputerName);
public sealed record EnrollAgentResponse(Guid DeviceId, string AgentId, Guid KeyId, string Secret, string Status);
public sealed record ApproveDeviceRequest(Guid EmployeeId);
public sealed record RotateCredentialResponse(Guid KeyId, string Secret, DateTimeOffset ExpiresAtUtc);
public sealed record PendingDeviceResponse(Guid DeviceId, string ComputerName, string Status, DateTimeOffset CreatedAtUtc);
public sealed record EmployeeOptionResponse(Guid Id, string DisplayName, string Group);
