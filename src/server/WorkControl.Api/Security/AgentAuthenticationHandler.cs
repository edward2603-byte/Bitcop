using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;

namespace WorkControl.Api.Security;

public static class AgentAuthenticationDefaults
{
    public const string Scheme = "WorkControlAgent";
}

public sealed class AgentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    WorkControlDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("WorkControl ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();
        var parts = header[12..].Split('.', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var keyId) ||
            string.IsNullOrWhiteSpace(parts[1]) || parts[1].Length > 128)
            return AuthenticateResult.Fail("Некорректные учётные данные агента.");

        var credential = await db.AgentCredentials.AsNoTracking().Include(x => x.Device)
            .SingleOrDefaultAsync(x => x.KeyId == keyId, Context.RequestAborted);
        var now = DateTimeOffset.UtcNow;
        if (credential?.Device is null || credential.RevokedAtUtc != null || credential.ExpiresAtUtc <= now ||
            !CryptographicOperations.FixedTimeEquals(credential.SecretHash, AgentSecurityService.HashText(parts[1])))
            return AuthenticateResult.Fail("Учётные данные агента отклонены.");

        if (credential.Device.Status is DeviceStatuses.Revoked or DeviceStatuses.Suspended)
            return AuthenticateResult.Fail("Устройство отключено.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, credential.DeviceId.ToString()),
            new Claim("credential_id", credential.Id.ToString()),
            new Claim("device_status", credential.Device.Status)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
