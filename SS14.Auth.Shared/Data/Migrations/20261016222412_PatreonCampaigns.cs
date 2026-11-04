using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class PatreonCampaigns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing patrons have to relink
            migrationBuilder.Sql("DELETE FROM \"Patrons\";");

            migrationBuilder.DropColumn(
                name: "CurrentTier",
                table: "Patrons");

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "PatreonWebhookLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatreonCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    PatreonCampaignId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TierMap = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    TierNames = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    DisabledTierIds = table.Column<string[]>(type: "text[]", nullable: false),
                    WebhookSecret = table.Column<string>(type: "text", nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ServerTokenHash = table.Column<string>(type: "text", nullable: true),
                    IsFirstParty = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastFullSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastWebhookAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatreonCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatreonCampaigns_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PatreonMemberships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    PatreonUserId = table.Column<string>(type: "text", nullable: false),
                    PatreonMemberId = table.Column<string>(type: "text", nullable: false),
                    PatronStatus = table.Column<string>(type: "text", nullable: true),
                    EntitledTierIds = table.Column<string[]>(type: "text[]", nullable: false),
                    CurrentTier = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatreonMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatreonMemberships_PatreonCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "PatreonCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatreonWebhookLogs_CampaignId",
                table: "PatreonWebhookLogs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PatreonCampaigns_IsFirstParty",
                table: "PatreonCampaigns",
                column: "IsFirstParty",
                unique: true,
                filter: "\"IsFirstParty\"");

            migrationBuilder.CreateIndex(
                name: "IX_PatreonCampaigns_Key",
                table: "PatreonCampaigns",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatreonCampaigns_OwnerUserId",
                table: "PatreonCampaigns",
                column: "OwnerUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatreonCampaigns_PatreonCampaignId",
                table: "PatreonCampaigns",
                column: "PatreonCampaignId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatreonCampaigns_ServerTokenHash",
                table: "PatreonCampaigns",
                column: "ServerTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatreonMemberships_CampaignId_PatreonUserId",
                table: "PatreonMemberships",
                columns: new[] { "CampaignId", "PatreonUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatreonMemberships_PatreonUserId",
                table: "PatreonMemberships",
                column: "PatreonUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatreonWebhookLogs_PatreonCampaigns_CampaignId",
                table: "PatreonWebhookLogs",
                column: "CampaignId",
                principalTable: "PatreonCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatreonWebhookLogs_PatreonCampaigns_CampaignId",
                table: "PatreonWebhookLogs");

            migrationBuilder.DropTable(
                name: "PatreonMemberships");

            migrationBuilder.DropTable(
                name: "PatreonCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_PatreonWebhookLogs_CampaignId",
                table: "PatreonWebhookLogs");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "PatreonWebhookLogs");

            migrationBuilder.AddColumn<string>(
                name: "CurrentTier",
                table: "Patrons",
                type: "text",
                nullable: true);
        }
    }
}
