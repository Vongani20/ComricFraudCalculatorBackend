using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComricFraudCalculatorBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Signals",
                columns: table => new
                {
                    SignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdNumberHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SignalType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SignalCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AggregateRiskScore = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.SignalId);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    ActivityLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.ActivityLogId);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_HrEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MnoEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Msisdn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationChannel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OutletOrDealer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceImei = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    FlagReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MnoEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_MnoEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_TenantId",
                table: "ActivityLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEvents_EventDate",
                table: "HrEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_HrEvents_TenantId",
                table: "HrEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MnoEvents_EventDate",
                table: "MnoEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_MnoEvents_TenantId",
                table: "MnoEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_IdNumberHash",
                table: "Signals",
                column: "IdNumberHash");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_IdNumberHash_SignalType_SignalCategory",
                table: "Signals",
                columns: new[] { "IdNumberHash", "SignalType", "SignalCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Signals_IsActive",
                table: "Signals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                table: "Tenants",
                column: "TenantCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "HrEvents");

            migrationBuilder.DropTable(
                name: "MnoEvents");

            migrationBuilder.DropTable(
                name: "Signals");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
