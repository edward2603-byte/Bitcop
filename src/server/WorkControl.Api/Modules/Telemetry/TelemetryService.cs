using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;

namespace WorkControl.Api.Modules.Telemetry;

public sealed class TelemetryService(WorkControlDbContext db)
{
    public async Task<AgentEventResponse> AcceptAsync(AgentEventRequest request, CancellationToken cancellationToken)
    {
        var duplicate = await db.AgentEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken);
        var receivedAt = DateTimeOffset.UtcNow;
        if (duplicate) return new AgentEventResponse(request.EventId, true, receivedAt);

        var device = await db.Devices.SingleOrDefaultAsync(x => x.AgentId == request.AgentId, cancellationToken)
            ?? throw new KeyNotFoundException("Устройство не зарегистрировано.");
        db.AgentEvents.Add(new AgentEvent
        {
            Id = Guid.NewGuid(), DeviceId = device.Id, EventId = request.EventId, EventType = request.EventType,
            OccurredAtUtc = request.OccurredAtUtc, ReceivedAtUtc = receivedAt, PayloadJson = request.PayloadJson
        });
        device.LastSeenAtUtc = receivedAt;
        await db.SaveChangesAsync(cancellationToken);
        return new AgentEventResponse(request.EventId, false, receivedAt);
    }
}
