using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;

namespace WorkControl.Api.Security;

public sealed class AuditService(WorkControlDbContext db)
{
    public async Task WriteAsync(string action, bool success, Guid? actorUserId = null, string? details = null, CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), ActorUserId = actorUserId, Action = action, Success = success,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = string.IsNullOrWhiteSpace(details) ? null : details[..Math.Min(details.Length, 1024)]
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AgentSecurityService(WorkControlDbContext db, IConfiguration configuration, AuditService audit)
{
    public async Task<CreateEnrollmentResponse> CreateEnrollmentAsync(Guid userId, CancellationToken ct)
    {
        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(configuration.GetValue("Security:EnrollmentMinutes", 10), 1, 60));
        db.DeviceEnrollments.Add(new DeviceEnrollment
        {
            Id = Guid.NewGuid(), CodeHash = HashText(code), CreatedAtUtc = now, ExpiresAtUtc = expires,
            CreatedByUserId = userId
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("agent.enrollment.created", true, userId, ct: ct);
        return new CreateEnrollmentResponse(code, expires);
    }

    public async Task<EnrollAgentResponse?> EnrollAsync(EnrollAgentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 256 ||
            string.IsNullOrWhiteSpace(request.ComputerName) || request.ComputerName.Length > 128)
        {
            await audit.WriteAsync("agent.enrollment.rejected", false, details: "invalid-input", ct: ct);
            return null;
        }
        var now = DateTimeOffset.UtcNow;
        var hash = HashText(request.Code.Trim());
        var candidates = await db.DeviceEnrollments.Where(x => x.UsedAtUtc == null).ToListAsync(ct);
        var enrollment = candidates.FirstOrDefault(x => x.ExpiresAtUtc > now && CryptographicOperations.FixedTimeEquals(x.CodeHash, hash));
        if (enrollment is null)
        {
            await audit.WriteAsync("agent.enrollment.rejected", false, details: "invalid-or-expired", ct: ct);
            return null;
        }

        var device = new Device
        {
            Id = Guid.NewGuid(), AgentId = $"device-{Guid.NewGuid():N}", ComputerName = request.ComputerName.Trim(),
            EmployeeId = null, Status = DeviceStatuses.Pending
        };
        var issued = IssueCredential(device.Id, now);
        enrollment.UsedAtUtc = now;
        db.Devices.Add(device);
        db.AgentCredentials.Add(issued.Entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("agent.enrolled", true, details: $"device={device.Id}", ct: ct);
        return new EnrollAgentResponse(device.Id, device.AgentId, issued.Entity.KeyId, issued.Secret, device.Status);
    }

    public async Task<bool> ApproveAsync(Guid deviceId, Guid employeeId, Guid actorId, CancellationToken ct)
    {
        var device = await db.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, ct);
        var employeeExists = await db.Employees.AnyAsync(x => x.Id == employeeId && !x.IsArchived, ct);
        if (device is null || !employeeExists || device.Status == DeviceStatuses.Revoked) return false;
        device.EmployeeId = employeeId;
        device.Status = DeviceStatuses.Active;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("agent.approved", true, actorId, $"device={deviceId}", ct);
        return true;
    }

    public async Task<bool> RevokeAsync(Guid deviceId, Guid actorId, CancellationToken ct)
    {
        var device = await db.Devices.Include(x => x.Credentials).SingleOrDefaultAsync(x => x.Id == deviceId, ct);
        if (device is null) return false;
        var now = DateTimeOffset.UtcNow;
        device.Status = DeviceStatuses.Revoked;
        foreach (var credential in device.Credentials.Where(x => x.RevokedAtUtc == null)) credential.RevokedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("agent.revoked", true, actorId, $"device={deviceId}", ct);
        return true;
    }

    public async Task<RotateCredentialResponse?> RotateAsync(Guid deviceId, Guid credentialId, CancellationToken ct)
    {
        var current = await db.AgentCredentials.SingleOrDefaultAsync(x => x.Id == credentialId && x.DeviceId == deviceId, ct);
        if (current is null || current.RevokedAtUtc != null) return null;
        var now = DateTimeOffset.UtcNow;
        var issued = IssueCredential(deviceId, now);
        current.RevokedAtUtc = now;
        db.AgentCredentials.Add(issued.Entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("agent.credential.rotated", true, details: $"device={deviceId}", ct: ct);
        return new RotateCredentialResponse(issued.Entity.KeyId, issued.Secret, issued.Entity.ExpiresAtUtc);
    }

    private (AgentCredential Entity, string Secret) IssueCredential(Guid deviceId, DateTimeOffset now)
    {
        var secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (new AgentCredential
        {
            Id = Guid.NewGuid(), KeyId = Guid.NewGuid(), DeviceId = deviceId, SecretHash = HashText(secret),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Math.Clamp(configuration.GetValue("Security:AgentCredentialDays", 90), 1, 365))
        }, secret);
    }

    public static byte[] HashText(string value) => SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
}
