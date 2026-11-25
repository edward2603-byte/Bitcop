using Microsoft.AspNetCore.Identity;

namespace WorkControl.Api.Domain;

public static class AppRoles
{
    public const string Admin = "ADMIN";
    public const string MainUser = "MAIN_USER";
    public const string GroupManager = "GROUP_MANAGER";
    public static readonly string[] All = [Admin, MainUser, GroupManager];
}

public static class DeviceStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Revoked = "Revoked";
}

public sealed class AppUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public ICollection<UserGroupAccess> GroupAccess { get; } = new List<UserGroupAccess>();
}

public sealed class UserGroupAccess
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WorkGroupId { get; set; }
    public AppUser? User { get; set; }
    public WorkGroup? WorkGroup { get; set; }
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ICollection<WorkGroup> Groups { get; } = new List<WorkGroup>();
}

public sealed class WorkGroup
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<Employee> Employees { get; } = new List<Employee>();
}

public sealed class Employee
{
    public Guid Id { get; set; }
    public Guid WorkGroupId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsArchived { get; set; }
    public WorkGroup? WorkGroup { get; set; }
    public ICollection<Device> Devices { get; } = new List<Device>();
}

public sealed class Device
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public required string AgentId { get; set; }
    public required string ComputerName { get; set; }
    public string Status { get; set; } = DeviceStatuses.Pending;
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<AgentEvent> Events { get; } = new List<AgentEvent>();
    public ICollection<AgentCredential> Credentials { get; } = new List<AgentCredential>();
}

public sealed class DeviceEnrollment
{
    public Guid Id { get; set; }
    public required byte[] CodeHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class AgentCredential
{
    public Guid Id { get; set; }
    public Guid KeyId { get; set; }
    public Guid DeviceId { get; set; }
    public required byte[] SecretHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Device? Device { get; set; }
}

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string Action { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? Details { get; set; }
}

public sealed class AgentEvent
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid EventId { get; set; }
    public required string EventType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? PayloadJson { get; set; }
    public Device? Device { get; set; }
}
