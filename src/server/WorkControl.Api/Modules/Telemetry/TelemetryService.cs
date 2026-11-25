using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;

namespace WorkControl.Api.Modules.Telemetry;

public sealed class TelemetryService(WorkControlDbContext db)
{
    public async Task<AgentEventResponse> AcceptAsync(Guid deviceId, AgentEventRequest request, CancellationToken ct)
    {
        if (request.EventId == Guid.Empty) throw new ArgumentException("Не задан EventId.");
        if (string.IsNullOrWhiteSpace(request.EventType) || request.EventType.Length > 64) throw new ArgumentException("Некорректный тип события.");
        if (request.OccurredAtUtc > DateTimeOffset.UtcNow.AddMinutes(5) || request.OccurredAtUtc < DateTimeOffset.UtcNow.AddDays(-7))
            throw new ArgumentException("Время события находится вне допустимого диапазона.");
        if (request.PayloadJson is not null && Encoding.UTF8.GetByteCount(request.PayloadJson) > 64 * 1024)
            throw new ArgumentException("Payload превышает 64 КБ.");
        if (request.PayloadJson is not null)
        {
            try { using var _ = JsonDocument.Parse(request.PayloadJson); }
            catch (JsonException) { throw new ArgumentException("PayloadJson содержит некорректный JSON."); }
        }

        var duplicate = await db.AgentEvents.AnyAsync(x => x.EventId == request.EventId, ct);
        var receivedAt = DateTimeOffset.UtcNow;
        if (duplicate) return new AgentEventResponse(request.EventId, true, receivedAt);

        var device = await db.Devices.SingleOrDefaultAsync(
            x => x.Id == deviceId && x.Status == DeviceStatuses.Active, ct)
            ?? throw new KeyNotFoundException("Устройство не одобрено или отключено.");
        db.AgentEvents.Add(new AgentEvent
        {
            Id = Guid.NewGuid(), DeviceId = device.Id, EventId = request.EventId, EventType = request.EventType,
            OccurredAtUtc = request.OccurredAtUtc, ReceivedAtUtc = receivedAt, PayloadJson = request.PayloadJson
        });
        device.LastSeenAtUtc = receivedAt;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            if (await db.AgentEvents.AsNoTracking().AnyAsync(x => x.EventId == request.EventId, ct))
                return new AgentEventResponse(request.EventId, true, DateTimeOffset.UtcNow);
            throw;
        }
        return new AgentEventResponse(request.EventId, false, receivedAt);
    }
}
