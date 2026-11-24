using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Contracts;
using WorkControl.Api.Infrastructure;
using WorkControl.Api.Modules.Devices;
using WorkControl.Api.Modules.Telemetry;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("WorkControl")
    ?? throw new InvalidOperationException("Не задана строка подключения WorkControl.");
builder.Services.AddDbContext<WorkControlDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)) options.UseNpgsql(connectionString);
    else options.UseSqlite(connectionString);
});
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
    if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();
    await DemoData.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/devices/register", async (RegisterDeviceRequest request, DeviceService service, CancellationToken ct) =>
    Results.Ok(await service.RegisterAsync(request, ct)));

app.MapPost("/api/telemetry/events", async (AgentEventRequest request, TelemetryService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.AcceptAsync(request, ct)); }
    catch (KeyNotFoundException error) { return Results.NotFound(new { message = error.Message }); }
});

app.MapGet("/api/dashboard", async (WorkControlDbContext db, IConfiguration configuration, CancellationToken ct) =>
{
    var devices = await db.Devices.AsNoTracking()
        .Include(x => x.Employee)!.ThenInclude(x => x!.WorkGroup)
        .Include(x => x.Events)
        .OrderBy(x => x.Employee!.DisplayName)
        .ToListAsync(ct);

    var now = DateTimeOffset.UtcNow;
    var localStartOfDay = DateTimeOffset.Now.Date;
    var onlineThresholdSeconds = Math.Max(1, configuration.GetValue("Presence:OnlineThresholdSeconds", 90));
    var maximumSignalGapSeconds = Math.Max(1, configuration.GetValue("Telemetry:MaximumSignalGapSeconds", 90));
    var rows = devices.Select(device =>
    {
        var summary = ActivityMetrics.GetActivitySummary(
            device.Events,
            localStartOfDay,
            now,
            maximumSignalGapSeconds);
        return new DashboardRow(
            device.Employee!.DisplayName,
            device.Employee.WorkGroup!.Name,
            device.ComputerName,
            device.LastSeenAtUtc,
            device.LastSeenAtUtc != null && device.LastSeenAtUtc > now.AddSeconds(-onlineThresholdSeconds)
                ? "На связи"
                : "Нет связи",
            summary.ObservedSeconds,
            summary.ActiveSeconds,
            ActivityMetrics.GetActivityState(device.Events));
    }).ToList();
    return Results.Ok(rows);
});

app.Run();

public partial class Program { }
