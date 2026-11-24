using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Domain;

namespace WorkControl.Api.Infrastructure;

public sealed class WorkControlDbContext(DbContextOptions<WorkControlDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<WorkGroup> WorkGroups => Set<WorkGroup>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AgentEvent> AgentEvents => Set<AgentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });
        modelBuilder.Entity<WorkGroup>(entity =>
        {
            entity.ToTable("WorkGroups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.HasOne(x => x.Organization).WithMany(x => x.Groups).HasForeignKey(x => x.OrganizationId);
        });
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasOne(x => x.WorkGroup).WithMany(x => x.Employees).HasForeignKey(x => x.WorkGroupId);
        });
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AgentId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ComputerName).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.AgentId).IsUnique();
            entity.HasOne(x => x.Employee).WithMany(x => x.Devices).HasForeignKey(x => x.EmployeeId);
        });
        modelBuilder.Entity<AgentEvent>(entity =>
        {
            entity.ToTable("AgentEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasOne(x => x.Device).WithMany(x => x.Events).HasForeignKey(x => x.DeviceId);
        });
    }
}
