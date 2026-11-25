using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkControl.Api.Infrastructure;

#nullable disable

namespace WorkControl.Api.Migrations;

[DbContext(typeof(WorkControlDbContext))]
[Migration("20261124125922_RestoreLegacyAgentEvents")]
public sealed class RestoreLegacyAgentEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Sqlite", StringComparison.Ordinal)) return;
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AgentEvents" ("Id", "DeviceId", "EventId", "EventType", "OccurredAtUtc", "ReceivedAtUtc", "PayloadJson")
            SELECT "Id", "DeviceId", "EventId", "EventType", "OccurredAtUtc", "ReceivedAtUtc", "PayloadJson" FROM "__AgentEventsBackup";
            DROP TABLE "__AgentEventsBackup";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Восстановление legacy-событий необратимо не удаляет данные при откате.
    }
}
