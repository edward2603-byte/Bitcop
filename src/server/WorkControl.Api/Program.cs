using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;
using WorkControl.Api.Modules.Telemetry;
using WorkControl.Api.Security;

var builder = WebApplication.CreateBuilder(args);
var mode = builder.Configuration["WorkControl:Mode"] ?? "Local";
var isPilot = string.Equals(mode, "Pilot", StringComparison.OrdinalIgnoreCase);
var configuredKeyPath = builder.Configuration["Security:DataProtectionKeyPath"];
var keyPath = configuredKeyPath
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorkControl", "keys");

if (isPilot)
{
    var urls = builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["Urls"] ?? string.Empty;
    var pilotHost = builder.Configuration["Security:PilotHost"];
    var certificatePath = builder.Configuration["Kestrel:Certificates:Default:Path"]
        ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Path"];
    var certificatePassword = builder.Configuration["Kestrel:Certificates:Default:Password"]
        ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Password"];
    var parsedUrls = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null).ToArray();
    var validDns = !string.IsNullOrWhiteSpace(pilotHost) &&
        !System.Net.IPAddress.TryParse(pilotHost, out _) && Uri.CheckHostName(pilotHost) == UriHostNameType.Dns;
    var validUrls = parsedUrls.Length > 0 && parsedUrls.All(uri => uri is not null &&
        uri.Scheme == Uri.UriSchemeHttps && string.Equals(uri.Host, pilotHost, StringComparison.OrdinalIgnoreCase));
    var contentRootPath = Path.GetFullPath(builder.Environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar);
    var contentRoot = contentRootPath + Path.DirectorySeparatorChar;
    var validKeyPath = !string.IsNullOrWhiteSpace(configuredKeyPath) && Path.IsPathRooted(configuredKeyPath);
    var fullKeyPath = validKeyPath ? Path.GetFullPath(configuredKeyPath!) : string.Empty;
    validKeyPath = validKeyPath && !string.Equals(fullKeyPath.TrimEnd(Path.DirectorySeparatorChar), contentRootPath, StringComparison.OrdinalIgnoreCase) &&
        !fullKeyPath.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase);
    var certificateExtension = Path.GetExtension(certificatePath ?? string.Empty);
    var validCertificate = !string.IsNullOrWhiteSpace(certificatePath) && Path.IsPathRooted(certificatePath) &&
        File.Exists(certificatePath) && (certificateExtension.Equals(".pfx", StringComparison.OrdinalIgnoreCase) ||
        certificateExtension.Equals(".p12", StringComparison.OrdinalIgnoreCase)) &&
        !Path.GetFullPath(certificatePath).StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(certificatePassword);
    if (!validDns || !validUrls || !validKeyPath || !validCertificate)
        throw new InvalidOperationException("PILOT запускается только с совпадающими HTTPS/DNS, внешним постоянным key ring и защищённым PFX/P12.");
    keyPath = fullKeyPath;
}
Directory.CreateDirectory(keyPath);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 128 * 1024);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("WorkControl")
    ?? throw new InvalidOperationException("Не задана строка подключения WorkControl.");
builder.Services.AddDbContext<WorkControlDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)) options.UseNpgsql(connectionString);
    else options.UseSqlite(connectionString);
});
var dataProtection = builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath)).SetApplicationName("WorkControl");
if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<WorkControlDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
});
authentication.AddIdentityCookies();
authentication.AddScheme<AuthenticationSchemeOptions, AgentAuthenticationHandler>(AgentAuthenticationDefaults.Scheme, _ => { });
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "WorkControl.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = isPilot ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    options.Events.OnValidatePrincipal = async context =>
    {
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        var user = await users.GetUserAsync(context.Principal!);
        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("ActiveAgent", policy =>
    {
        policy.AddAuthenticationSchemes(AgentAuthenticationDefaults.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("device_status", DeviceStatuses.Active);
    });
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "WorkControl.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = isPilot ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
    options.AddFixedWindowLimiter("enroll", limiter => { limiter.PermitLimit = 5; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<AgentSecurityService>();
builder.Services.AddScoped<AuditService>();
if (!isPilot)
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseRateLimiter();
if (!isPilot) app.UseCors();
if (isPilot) { app.UseHsts(); app.UseHttpsRedirection(); app.UseDefaultFiles(); app.UseStaticFiles(); }
app.UseAuthentication();
app.UseAuthorization();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    await DatabaseBootstrap.InitializeAsync(app.Services, seedLocalDemo: !isPilot);

if (args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase))
{
    var email = Environment.GetEnvironmentVariable("WORKCONTROL_BOOTSTRAP_EMAIL") ?? "admin@workcontrol.local";
    var password = Environment.GetEnvironmentVariable("WORKCONTROL_BOOTSTRAP_PASSWORD");
    if (string.IsNullOrWhiteSpace(password))
    {
        Console.Error.WriteLine("Задайте пароль только через WORKCONTROL_BOOTSTRAP_PASSWORD.");
        Environment.ExitCode = 1;
        return;
    }
    Environment.ExitCode = await DatabaseBootstrap.BootstrapAdminAsync(app.Services, email, password);
    return;
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", mode }));
app.MapGet("/api/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
});
app.MapPost("/api/auth/login", async (LoginRequest request, HttpContext context, IAntiforgery antiforgery,
    SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AuditService audit, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 256 ||
        string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 256)
    {
        await audit.WriteAsync("user.login", false, details: "invalid-input", ct: ct);
        return Results.BadRequest(new { message = "Некорректные данные входа." });
    }
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null || !user.IsActive)
    {
        await audit.WriteAsync("user.login", false, details: "unknown-or-disabled", ct: ct);
        return Results.Unauthorized();
    }
    var result = await signInManager.PasswordSignInAsync(user, request.Password, false, lockoutOnFailure: true);
    await audit.WriteAsync("user.login", result.Succeeded, user.Id, result.IsLockedOut ? "locked" : null, ct);
    return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
}).RequireRateLimiting("login");
app.MapPost("/api/auth/logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<AppUser> signInManager,
    AuditService audit, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var actor = GetUserId(context.User);
    await signInManager.SignOutAsync();
    await audit.WriteAsync("user.logout", true, actor, ct: ct);
    return Results.NoContent();
}).RequireAuthorization();
app.MapGet("/api/auth/me", async (ClaimsPrincipal principal, UserManager<AppUser> userManager) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null || !user.IsActive) return Results.Unauthorized();
    return Results.Ok(new CurrentUserResponse(user.Id, user.Email ?? user.UserName ?? string.Empty, (await userManager.GetRolesAsync(user)).ToArray()));
}).RequireAuthorization();

app.MapGet("/api/dashboard", async (Guid? groupId, ClaimsPrincipal principal, UserManager<AppUser> userManager,
    WorkControlDbContext db, IConfiguration configuration, CancellationToken ct) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null || !user.IsActive) return Results.Unauthorized();
    var roles = await userManager.GetRolesAsync(user);
    var query = db.Devices.AsNoTracking().Where(x => x.EmployeeId != null && x.Status == DeviceStatuses.Active);
    if (roles.Contains(AppRoles.GroupManager))
    {
        var allowed = db.UserGroupAccess.Where(x => x.UserId == user.Id).Select(x => x.WorkGroupId);
        if (groupId.HasValue && !await allowed.ContainsAsync(groupId.Value, ct)) return Results.Forbid();
        query = query.Where(x => allowed.Contains(x.Employee!.WorkGroupId));
    }
    if (groupId.HasValue) query = query.Where(x => x.Employee!.WorkGroupId == groupId.Value);
    var devices = await query.Include(x => x.Employee)!.ThenInclude(x => x!.WorkGroup).Include(x => x.Events)
        .OrderBy(x => x.Employee!.DisplayName).ToListAsync(ct);
    var now = DateTimeOffset.UtcNow;
    var start = DateTimeOffset.Now.Date;
    var onlineSeconds = Math.Max(1, configuration.GetValue("Presence:OnlineThresholdSeconds", 90));
    var gapSeconds = Math.Max(1, configuration.GetValue("Telemetry:MaximumSignalGapSeconds", 90));
    var rows = devices.Select(device =>
    {
        var summary = ActivityMetrics.GetActivitySummary(device.Events, start, now, gapSeconds);
        return new DashboardRow(device.Employee!.DisplayName, device.Employee.WorkGroup!.Name, device.ComputerName,
            device.LastSeenAtUtc, device.LastSeenAtUtc > now.AddSeconds(-onlineSeconds) ? "На связи" : "Нет связи",
            summary.ObservedSeconds, summary.ActiveSeconds, ActivityMetrics.GetActivityState(device.Events));
    });
    return Results.Ok(rows);
}).RequireAuthorization();

app.MapPost("/api/admin/enrollments", async (HttpContext context, IAntiforgery antiforgery, AgentSecurityService service, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    return Results.Ok(await service.CreateEnrollmentAsync(GetUserId(context.User)!.Value, ct));
}).RequireAuthorization("Admin");
app.MapGet("/api/admin/devices/pending", async (WorkControlDbContext db, CancellationToken ct) =>
{
    // SQLite cannot translate MIN(DateTimeOffset) consistently. Pending devices are an
    // intentionally small admin queue, so calculate the credential issuance time after
    // the filtered query without changing the persisted schema.
    var devices = await db.Devices.AsNoTracking()
        .Where(x => x.Status == DeviceStatuses.Pending)
        .Include(x => x.Credentials)
        .OrderBy(x => x.ComputerName)
        .ToListAsync(ct);
    return Results.Ok(devices.Select(x => new PendingDeviceResponse(
        x.Id,
        x.ComputerName,
        x.Status,
        x.Credentials.OrderBy(c => c.CreatedAtUtc).Select(c => c.CreatedAtUtc).FirstOrDefault())));
}).RequireAuthorization("Admin");
app.MapGet("/api/admin/employees", async (WorkControlDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Employees.AsNoTracking().Where(x => !x.IsArchived)
        .OrderBy(x => x.DisplayName)
        .Select(x => new EmployeeOptionResponse(x.Id, x.DisplayName, x.WorkGroup!.Name)).ToListAsync(ct)))
    .RequireAuthorization("Admin");
app.MapPost("/api/admin/devices/{deviceId:guid}/approve", async (Guid deviceId, ApproveDeviceRequest request, HttpContext context,
    IAntiforgery antiforgery, AgentSecurityService service, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    return await service.ApproveAsync(deviceId, request.EmployeeId, GetUserId(context.User)!.Value, ct) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("Admin");
app.MapPost("/api/admin/devices/{deviceId:guid}/revoke", async (Guid deviceId, HttpContext context, IAntiforgery antiforgery,
    AgentSecurityService service, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    return await service.RevokeAsync(deviceId, GetUserId(context.User)!.Value, ct) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("Admin");

app.MapPost("/api/agent/v1/enroll", async (EnrollAgentRequest request, AgentSecurityService service, CancellationToken ct) =>
{
    var result = await service.EnrollAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).RequireRateLimiting("enroll");
app.MapPost("/api/agent/v1/events", async (AgentEventRequest request, ClaimsPrincipal principal, TelemetryService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.AcceptAsync(GetUserId(principal)!.Value, request, ct)); }
    catch (ArgumentException error) { return Results.BadRequest(new { message = error.Message }); }
    catch (KeyNotFoundException) { return Results.Forbid(); }
}).RequireAuthorization("ActiveAgent");
app.MapPost("/api/agent/v1/credentials/rotate", async (ClaimsPrincipal principal, AgentSecurityService service, CancellationToken ct) =>
{
    var deviceId = GetUserId(principal)!.Value;
    var credentialId = Guid.Parse(principal.FindFirstValue("credential_id")!);
    var result = await service.RotateAsync(deviceId, credentialId, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).RequireAuthorization("ActiveAgent");

if (isPilot) app.MapFallbackToFile("index.html");
app.Run();

static Guid? GetUserId(ClaimsPrincipal principal) =>
    Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

public partial class Program { }
