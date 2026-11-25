using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SecurePilot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                UpPostgres(migrationBuilder);
                return;
            }
            migrationBuilder.Sql("""
                CREATE TABLE "__AgentEventsBackup" AS
                SELECT "Id", "DeviceId", "EventId", "EventType", "OccurredAtUtc", "ReceivedAtUtc", "PayloadJson" FROM "AgentEvents";
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Employees_EmployeeId",
                table: "Devices");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "Devices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Devices",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "AgentCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecretHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentCredentials_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodeHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRoleClaims_AppRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserClaims_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AppUserLogins_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AppUserRoles_AppRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserRoles_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AppUserTokens_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkGroupId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupAccess_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupAccess_WorkGroups_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_DeviceId",
                table: "AgentCredentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_KeyId",
                table: "AgentCredentials",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleClaims_RoleId",
                table: "AppRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AppRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserClaims_UserId",
                table: "AppUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserLogins_UserId",
                table: "AppUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserRoles_RoleId",
                table: "AppUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AppUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AppUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAtUtc",
                table: "AuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnrollments_CodeHash",
                table: "DeviceEnrollments",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupAccess_UserId_WorkGroupId",
                table: "UserGroupAccess",
                columns: new[] { "UserId", "WorkGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupAccess_WorkGroupId",
                table: "UserGroupAccess",
                column: "WorkGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Employees_EmployeeId",
                table: "Devices",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                DownPostgres(migrationBuilder);
                return;
            }
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Employees_EmployeeId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "AgentCredentials");

            migrationBuilder.DropTable(
                name: "AppRoleClaims");

            migrationBuilder.DropTable(
                name: "AppUserClaims");

            migrationBuilder.DropTable(
                name: "AppUserLogins");

            migrationBuilder.DropTable(
                name: "AppUserRoles");

            migrationBuilder.DropTable(
                name: "AppUserTokens");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "DeviceEnrollments");

            migrationBuilder.DropTable(
                name: "UserGroupAccess");

            migrationBuilder.DropTable(
                name: "AppRoles");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Devices");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Employees_EmployeeId",
                table: "Devices",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void UpPostgres(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Devices" DROP CONSTRAINT "FK_Devices_Employees_EmployeeId";
                ALTER TABLE "Devices" ALTER COLUMN "EmployeeId" DROP NOT NULL;
                ALTER TABLE "Devices" ADD COLUMN "Status" character varying(16) NOT NULL DEFAULT 'Active';
                ALTER TABLE "Devices" ADD CONSTRAINT "FK_Devices_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees"("Id") ON DELETE SET NULL;

                CREATE TABLE "AppRoles" (
                    "Id" uuid PRIMARY KEY, "Name" character varying(256), "NormalizedName" character varying(256), "ConcurrencyStamp" text);
                CREATE TABLE "AppUsers" (
                    "Id" uuid PRIMARY KEY, "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "UserName" character varying(256), "NormalizedUserName" character varying(256),
                    "Email" character varying(256), "NormalizedEmail" character varying(256), "EmailConfirmed" boolean NOT NULL,
                    "PasswordHash" text, "SecurityStamp" text, "ConcurrencyStamp" text, "PhoneNumber" text,
                    "PhoneNumberConfirmed" boolean NOT NULL, "TwoFactorEnabled" boolean NOT NULL,
                    "LockoutEnd" timestamp with time zone, "LockoutEnabled" boolean NOT NULL, "AccessFailedCount" integer NOT NULL);
                CREATE TABLE "AuditEvents" (
                    "Id" uuid PRIMARY KEY, "ActorUserId" uuid, "Action" character varying(96) NOT NULL,
                    "Success" boolean NOT NULL, "OccurredAtUtc" timestamp with time zone NOT NULL, "Details" character varying(1024));
                CREATE TABLE "DeviceEnrollments" (
                    "Id" uuid PRIMARY KEY, "CodeHash" bytea NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "ExpiresAtUtc" timestamp with time zone NOT NULL, "UsedAtUtc" timestamp with time zone, "CreatedByUserId" uuid NOT NULL);
                CREATE TABLE "AgentCredentials" (
                    "Id" uuid PRIMARY KEY, "KeyId" uuid NOT NULL, "DeviceId" uuid NOT NULL, "SecretHash" bytea NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL, "ExpiresAtUtc" timestamp with time zone NOT NULL,
                    "RevokedAtUtc" timestamp with time zone,
                    CONSTRAINT "FK_AgentCredentials_Devices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES "Devices"("Id") ON DELETE CASCADE);
                CREATE TABLE "AppRoleClaims" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "RoleId" uuid NOT NULL, "ClaimType" text, "ClaimValue" text,
                    CONSTRAINT "FK_AppRoleClaims_AppRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AppRoles"("Id") ON DELETE CASCADE);
                CREATE TABLE "AppUserClaims" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "UserId" uuid NOT NULL, "ClaimType" text, "ClaimValue" text,
                    CONSTRAINT "FK_AppUserClaims_AppUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUsers"("Id") ON DELETE CASCADE);
                CREATE TABLE "AppUserLogins" (
                    "LoginProvider" text NOT NULL, "ProviderKey" text NOT NULL, "ProviderDisplayName" text, "UserId" uuid NOT NULL,
                    CONSTRAINT "PK_AppUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
                    CONSTRAINT "FK_AppUserLogins_AppUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUsers"("Id") ON DELETE CASCADE);
                CREATE TABLE "AppUserRoles" (
                    "UserId" uuid NOT NULL, "RoleId" uuid NOT NULL, CONSTRAINT "PK_AppUserRoles" PRIMARY KEY ("UserId", "RoleId"),
                    CONSTRAINT "FK_AppUserRoles_AppRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AppRoles"("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_AppUserRoles_AppUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUsers"("Id") ON DELETE CASCADE);
                CREATE TABLE "AppUserTokens" (
                    "UserId" uuid NOT NULL, "LoginProvider" text NOT NULL, "Name" text NOT NULL, "Value" text,
                    CONSTRAINT "PK_AppUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
                    CONSTRAINT "FK_AppUserTokens_AppUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUsers"("Id") ON DELETE CASCADE);
                CREATE TABLE "UserGroupAccess" (
                    "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "WorkGroupId" uuid NOT NULL,
                    CONSTRAINT "FK_UserGroupAccess_AppUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AppUsers"("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_UserGroupAccess_WorkGroups_WorkGroupId" FOREIGN KEY ("WorkGroupId") REFERENCES "WorkGroups"("Id") ON DELETE CASCADE);

                CREATE INDEX "IX_AgentCredentials_DeviceId" ON "AgentCredentials" ("DeviceId");
                CREATE UNIQUE INDEX "IX_AgentCredentials_KeyId" ON "AgentCredentials" ("KeyId");
                CREATE INDEX "IX_AppRoleClaims_RoleId" ON "AppRoleClaims" ("RoleId");
                CREATE UNIQUE INDEX "RoleNameIndex" ON "AppRoles" ("NormalizedName");
                CREATE INDEX "IX_AppUserClaims_UserId" ON "AppUserClaims" ("UserId");
                CREATE INDEX "IX_AppUserLogins_UserId" ON "AppUserLogins" ("UserId");
                CREATE INDEX "IX_AppUserRoles_RoleId" ON "AppUserRoles" ("RoleId");
                CREATE INDEX "EmailIndex" ON "AppUsers" ("NormalizedEmail");
                CREATE UNIQUE INDEX "UserNameIndex" ON "AppUsers" ("NormalizedUserName");
                CREATE INDEX "IX_AuditEvents_OccurredAtUtc" ON "AuditEvents" ("OccurredAtUtc");
                CREATE UNIQUE INDEX "IX_DeviceEnrollments_CodeHash" ON "DeviceEnrollments" ("CodeHash");
                CREATE UNIQUE INDEX "IX_UserGroupAccess_UserId_WorkGroupId" ON "UserGroupAccess" ("UserId", "WorkGroupId");
                CREATE INDEX "IX_UserGroupAccess_WorkGroupId" ON "UserGroupAccess" ("WorkGroupId");
                """, suppressTransaction: false);
        }

        private static void DownPostgres(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE "AgentCredentials"; DROP TABLE "AppRoleClaims"; DROP TABLE "AppUserClaims";
                DROP TABLE "AppUserLogins"; DROP TABLE "AppUserRoles"; DROP TABLE "AppUserTokens";
                DROP TABLE "AuditEvents"; DROP TABLE "DeviceEnrollments"; DROP TABLE "UserGroupAccess";
                DROP TABLE "AppRoles"; DROP TABLE "AppUsers";
                ALTER TABLE "Devices" DROP CONSTRAINT "FK_Devices_Employees_EmployeeId";
                ALTER TABLE "Devices" DROP COLUMN "Status";
                ALTER TABLE "Devices" ALTER COLUMN "EmployeeId" SET NOT NULL;
                ALTER TABLE "Devices" ADD CONSTRAINT "FK_Devices_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees"("Id") ON DELETE CASCADE;
                """);
        }
    }
}
