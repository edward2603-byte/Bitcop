using WorkControl.Api.Contracts;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class AgentContractsTests
{
    [Fact]
    public void Control_event_preserves_idempotency_key_without_a_trusted_agent_id()
    {
        var eventId = Guid.NewGuid();
        var request = new AgentEventRequest(eventId, "control.heartbeat", DateTimeOffset.UtcNow, "{\"source\":\"test\"}");
        Assert.Equal(eventId, request.EventId);
        Assert.Equal("control.heartbeat", request.EventType);
        Assert.DoesNotContain("AgentId", request.GetType().GetProperties().Select(x => x.Name));
    }

    [Fact]
    public void Enrollment_contract_contains_no_employee_assignment()
    {
        var request = new EnrollAgentRequest("one-time-code", "PILOT-PC");
        Assert.Equal("PILOT-PC", request.ComputerName);
        Assert.DoesNotContain(request.GetType().GetProperties(), x => x.Name.Contains("Employee", StringComparison.Ordinal));
    }
}
