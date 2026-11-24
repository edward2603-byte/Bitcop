using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkControl.Api.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "Organizations", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Organizations", x => x.Id));
        migrationBuilder.CreateTable(name: "WorkGroups", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_WorkGroups", x => x.Id);
            table.ForeignKey("FK_WorkGroups_Organizations_OrganizationId", x => x.OrganizationId, "Organizations", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable(name: "Employees", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            WorkGroupId = table.Column<Guid>(type: "uuid", nullable: false),
            DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            IsArchived = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Employees", x => x.Id);
            table.ForeignKey("FK_Employees_WorkGroups_WorkGroupId", x => x.WorkGroupId, "WorkGroups", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable(name: "Devices", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
            AgentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
            ComputerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
            LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Devices", x => x.Id);
            table.ForeignKey("FK_Devices_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable(name: "AgentEvents", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
            EventId = table.Column<Guid>(type: "uuid", nullable: false),
            EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            PayloadJson = table.Column<string>(type: "jsonb", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AgentEvents", x => x.Id);
            table.ForeignKey("FK_AgentEvents_Devices_DeviceId", x => x.DeviceId, "Devices", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_WorkGroups_OrganizationId", table: "WorkGroups", column: "OrganizationId");
        migrationBuilder.CreateIndex(name: "IX_Employees_WorkGroupId", table: "Employees", column: "WorkGroupId");
        migrationBuilder.CreateIndex(name: "IX_Devices_AgentId", table: "Devices", column: "AgentId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Devices_EmployeeId", table: "Devices", column: "EmployeeId");
        migrationBuilder.CreateIndex(name: "IX_AgentEvents_DeviceId", table: "AgentEvents", column: "DeviceId");
        migrationBuilder.CreateIndex(name: "IX_AgentEvents_EventId", table: "AgentEvents", column: "EventId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AgentEvents");
        migrationBuilder.DropTable(name: "Devices");
        migrationBuilder.DropTable(name: "Employees");
        migrationBuilder.DropTable(name: "WorkGroups");
        migrationBuilder.DropTable(name: "Organizations");
    }
}
