using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Domain;

namespace WorkControl.Api.Infrastructure;

public sealed class WorkControlDbContext(DbContextOptions<WorkControlDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<WorkGroup> WorkGroups => Set<WorkGroup>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AgentEvent> AgentEvents => Set<AgentEvent>();
    public DbSet<UserGroupAccess> UserGroupAccess => Set<UserGroupAccess>();
    public DbSet<DeviceEnrollment> DeviceEnrollments => Set<DeviceEnrollment>();
    public DbSet<AgentCredential> AgentCredentials => Set<AgentCredential>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("AppRoles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("AppUserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AppUserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AppUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AppRoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AppUserTokens");
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
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.HasIndex(x => x.AgentId).IsUnique();
            entity.HasOne(x => x.Employee).WithMany(x => x.Devices).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
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
        modelBuilder.Entity<UserGroupAccess>(entity =>
        {
            entity.ToTable("UserGroupAccess");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.WorkGroupId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.GroupAccess).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.WorkGroup).WithMany().HasForeignKey(x => x.WorkGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DeviceEnrollment>(entity =>
        {
            entity.ToTable("DeviceEnrollments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CodeHash).IsRequired();
            entity.HasIndex(x => x.CodeHash).IsUnique();
        });
        modelBuilder.Entity<AgentCredential>(entity =>
        {
            entity.ToTable("AgentCredentials");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SecretHash).IsRequired();
            entity.HasIndex(x => x.KeyId).IsUnique();
            entity.HasOne(x => x.Device).WithMany(x => x.Credentials).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(96).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(1024);
            entity.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
