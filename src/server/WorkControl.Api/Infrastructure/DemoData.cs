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
        db.AddRange(organization, group);
        await db.SaveChangesAsync(cancellationToken);
    }
}
