using Microsoft.EntityFrameworkCore;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;

namespace WorkControl.Api.Modules.Devices;

public sealed class DeviceService(WorkControlDbContext db)
{
    public async Task<RegisterDeviceResponse> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId ?? await FindOrCreateEmployeeAsync(request, cancellationToken);
        var device = await db.Devices.SingleOrDefaultAsync(x => x.AgentId == request.AgentId, cancellationToken);
        if (device is null)
        {
            device = new Device { Id = Guid.NewGuid(), EmployeeId = employeeId, AgentId = request.AgentId, ComputerName = request.ComputerName };
            db.Devices.Add(device);
        }
        else
        {
            device.ComputerName = request.ComputerName;
            device.EmployeeId = employeeId;
        }
        device.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new RegisterDeviceResponse(device.Id, device.LastSeenAtUtc.Value);
    }

    private async Task<Guid> FindOrCreateEmployeeAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var displayName = string.IsNullOrWhiteSpace(request.EmployeeName)
            ? request.ComputerName
            : request.EmployeeName.Trim();
        var existingEmployee = await db.Employees
            .Where(x => x.DisplayName == displayName)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingEmployee is not null) return existingEmployee.Value;

        var workGroupId = await db.WorkGroups.Select(x => x.Id).FirstAsync(cancellationToken);
        var employee = new Employee { Id = Guid.NewGuid(), WorkGroupId = workGroupId, DisplayName = displayName };
        db.Employees.Add(employee);
        return employee.Id;
    }
}
