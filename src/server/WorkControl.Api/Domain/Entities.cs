namespace WorkControl.Api.Domain;

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
    public Guid EmployeeId { get; set; }
    public required string AgentId { get; set; }
    public required string ComputerName { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<AgentEvent> Events { get; } = new List<AgentEvent>();
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
