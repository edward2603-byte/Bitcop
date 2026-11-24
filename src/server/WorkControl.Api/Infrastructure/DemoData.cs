using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Domain;

namespace WorkControl.Api.Infrastructure;

public static class DemoData
{
    public static async Task SeedAsync(WorkControlDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Organizations.AnyAsync(cancellationToken)) return;

        var organization = new Organization { Id = Guid.NewGuid(), Name = "ООО «Проектный контур»" };
        var group = new WorkGroup { Id = Guid.NewGuid(), OrganizationId = organization.Id, Name = "Проектный отдел", Organization = organization };
        var employee = new Employee { Id = Guid.NewGuid(), WorkGroupId = group.Id, DisplayName = "Анна Демонстрационная", WorkGroup = group };
        var device = new Device { Id = Guid.NewGuid(), EmployeeId = employee.Id, AgentId = "demo-agent-001", ComputerName = "DESIGN-01", Employee = employee, LastSeenAtUtc = DateTimeOffset.UtcNow };

        db.AddRange(organization, group, employee, device);
        await db.SaveChangesAsync(cancellationToken);
    }
}
