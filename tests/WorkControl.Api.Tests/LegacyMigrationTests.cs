using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class LegacyMigrationTests
{
    [Fact]
    public async Task Legacy_sqlite_is_baselined_and_upgraded_without_losing_events()
    {
        var path = Path.Combine(Path.GetTempPath(), $"workcontrol-legacy-{Guid.NewGuid():N}.db");
        var organizationId = Guid.NewGuid(); var groupId = Guid.NewGuid(); var employeeId = Guid.NewGuid(); var deviceId = Guid.NewGuid(); var eventId = Guid.NewGuid();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE Organizations (Id TEXT PRIMARY KEY, Name TEXT NOT NULL);
                    CREATE TABLE WorkGroups (Id TEXT PRIMARY KEY, OrganizationId TEXT NOT NULL, Name TEXT NOT NULL);
                    CREATE TABLE Employees (Id TEXT PRIMARY KEY, WorkGroupId TEXT NOT NULL, DisplayName TEXT NOT NULL, IsArchived INTEGER NOT NULL);
                    CREATE TABLE Devices (Id TEXT PRIMARY KEY, EmployeeId TEXT NOT NULL, AgentId TEXT NOT NULL, ComputerName TEXT NOT NULL, LastSeenAtUtc TEXT NULL);
                    CREATE TABLE AgentEvents (Id TEXT PRIMARY KEY, DeviceId TEXT NOT NULL, EventId TEXT NOT NULL, EventType TEXT NOT NULL, OccurredAtUtc TEXT NOT NULL, ReceivedAtUtc TEXT NOT NULL, PayloadJson TEXT NULL);
                    CREATE UNIQUE INDEX IX_Devices_AgentId ON Devices(AgentId);
                    CREATE UNIQUE INDEX IX_AgentEvents_EventId ON AgentEvents(EventId);
                    INSERT INTO Organizations VALUES ($organization, 'Legacy');
                    INSERT INTO WorkGroups VALUES ($group, $organization, 'Legacy group');
                    INSERT INTO Employees VALUES ($employee, $group, 'Legacy employee', 0);
                    INSERT INTO Devices VALUES ($device, $employee, 'legacy-agent', 'LEGACY-PC', NULL);
                    INSERT INTO AgentEvents VALUES ($row, $device, $event, 'activity.status', $time, $time, '{"state":"active"}');
                    """;
                command.Parameters.AddWithValue("$organization", organizationId.ToString().ToUpperInvariant()); command.Parameters.AddWithValue("$group", groupId.ToString().ToUpperInvariant());
                command.Parameters.AddWithValue("$employee", employeeId.ToString().ToUpperInvariant()); command.Parameters.AddWithValue("$device", deviceId.ToString().ToUpperInvariant());
                command.Parameters.AddWithValue("$row", Guid.NewGuid().ToString().ToUpperInvariant()); command.Parameters.AddWithValue("$event", eventId.ToString().ToUpperInvariant());
                command.Parameters.AddWithValue("$time", DateTimeOffset.UtcNow.ToString("O")); await command.ExecuteNonQueryAsync();
            }
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:WorkControl", $"Data Source={path}")
                .UseSetting("Database:Provider", "Sqlite").UseSetting("Database:AutoMigrate", "true")
                .UseSetting("WorkControl:Mode", "Local")
                .UseSetting("Security:DataProtectionKeyPath", Path.Combine(Path.GetTempPath(), $"workcontrol-keys-{Guid.NewGuid():N}")));
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
            Assert.Equal(1, await db.AgentEvents.CountAsync(x => x.EventId == eventId));
            Assert.Equal(DeviceStatuses.Active, (await db.Devices.SingleAsync(x => x.Id == deviceId)).Status);
            Assert.True((await db.Database.GetAppliedMigrationsAsync()).Count() >= 3);
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }
}
