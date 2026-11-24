using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    .WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

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

app.MapGet("/api/dashboard", async (WorkControlDbContext db, CancellationToken ct) =>
{
    var devices = await db.Devices.AsNoTracking()
        .Include(x => x.Employee)!.ThenInclude(x => x!.WorkGroup)
        .Include(x => x.Events)
        .OrderBy(x => x.Employee!.DisplayName)
        .ToListAsync(ct);

    var now = DateTimeOffset.UtcNow;
    var localStartOfDay = DateTimeOffset.Now.Date;
    var rows = devices.Select(device => new DashboardRow(
        device.Employee!.DisplayName,
        device.Employee.WorkGroup!.Name,
        device.ComputerName,
        device.LastSeenAtUtc,
        device.LastSeenAtUtc != null && device.LastSeenAtUtc > now.AddMinutes(-5) ? "На связи" : "Нет связи",
        GetActivitySummary(device.Events, localStartOfDay, now).ObservedSeconds,
        GetActivitySummary(device.Events, localStartOfDay, now).ActiveSeconds,
        GetActivityState(device.Events))).ToList();
    return Results.Ok(rows);
});

app.Run();

static string GetActivityState(IEnumerable<WorkControl.Api.Domain.AgentEvent> events)
{
    var latest = events.Where(x => x.EventType == "activity.status").OrderByDescending(x => x.ReceivedAtUtc).FirstOrDefault();
    if (latest?.PayloadJson is null) return "Нет данных";
    try
    {
        var state = JsonDocument.Parse(latest.PayloadJson).RootElement.GetProperty("state").GetString();
        return state switch { "active" => "Активен", "idle" => "Неактивен", _ => "Нет данных" };
    }
    catch (JsonException) { return "Нет данных"; }
}

static ActivitySummary GetActivitySummary(IEnumerable<WorkControl.Api.Domain.AgentEvent> events, DateTimeOffset startOfDay, DateTimeOffset now)
{
    var activityEvents = events
        .Where(x => x.EventType == "activity.status")
        .OrderBy(x => x.OccurredAtUtc)
        .ToList();
    if (activityEvents.Count == 0) return new ActivitySummary(0, 0);

    long observedSeconds = 0;
    long activeSeconds = 0;
    const int maximumSignalGapSeconds = 90;
    for (var index = 0; index < activityEvents.Count; index++)
    {
        var current = activityEvents[index];
        var intervalStart = current.OccurredAtUtc < startOfDay ? startOfDay : current.OccurredAtUtc;
        var nextAt = index + 1 < activityEvents.Count ? activityEvents[index + 1].OccurredAtUtc : now;
        var intervalEnd = nextAt < now ? nextAt : now;
        var maximumEnd = current.OccurredAtUtc.AddSeconds(maximumSignalGapSeconds);
        if (intervalEnd > maximumEnd) intervalEnd = maximumEnd;
        if (intervalEnd <= intervalStart) continue;

        var seconds = (long)(intervalEnd - intervalStart).TotalSeconds;
        observedSeconds += seconds;
        if (GetActivityValue(current.PayloadJson) == "active") activeSeconds += seconds;
    }
    return new ActivitySummary(observedSeconds, activeSeconds);
}

static string? GetActivityValue(string? payloadJson)
{
    if (payloadJson is null) return null;
    try { return JsonDocument.Parse(payloadJson).RootElement.GetProperty("state").GetString(); }
    catch (JsonException) { return null; }
}

public sealed record ActivitySummary(long ObservedSeconds, long ActiveSeconds);

public partial class Program { }
