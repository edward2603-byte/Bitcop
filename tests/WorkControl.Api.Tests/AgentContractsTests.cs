using System;
using Xunit;
using WorkControl.Api.Contracts;

namespace WorkControl.Api.Tests;

public sealed class AgentContractsTests
{
    [Fact]
    public void Control_event_preserves_the_idempotency_key()
    {
        var eventId = Guid.NewGuid();
        var request = new AgentEventRequest(eventId, "agent-01", "control.heartbeat", DateTimeOffset.UtcNow, "{\"source\":\"test\"}");
        Assert.Equal(eventId, request.EventId);
        Assert.Equal("control.heartbeat", request.EventType);
    }

    [Fact]
    public void Device_registration_accepts_the_windows_user_name()
    {
        var request = new RegisterDeviceRequest("agent-01", "PILOT-PC", null, "pilot.user");
        Assert.Equal("pilot.user", request.EmployeeName);
    }
}
