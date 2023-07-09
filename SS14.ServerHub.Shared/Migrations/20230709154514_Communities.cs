using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class Communities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedAddress");

            migrationBuilder.DropTable(
                name: "BannedDomain");

            migrationBuilder.CreateTable(
                name: "TrackedCommunity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCommunity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackedCommunityAddress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: false),
                    TrackedCommunityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCommunityAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedCommunityAddress_TrackedCommunity_TrackedCommunityId",
                        column: x => x.TrackedCommunityId,
                        principalTable: "TrackedCommunity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackedCommunityDomain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DomainName = table.Column<string>(type: "text", nullable: false),
                    TrackedCommunityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCommunityDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedCommunityDomain_TrackedCommunity_TrackedCommunityId",
                        column: x => x.TrackedCommunityId,
                        principalTable: "TrackedCommunity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCommunityAddress_TrackedCommunityId",
                table: "TrackedCommunityAddress",
                column: "TrackedCommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCommunityDomain_TrackedCommunityId",
                table: "TrackedCommunityDomain",
                column: "TrackedCommunityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedCommunityAddress");

            migrationBuilder.DropTable(
                name: "TrackedCommunityDomain");

            migrationBuilder.DropTable(
                name: "TrackedCommunity");

            migrationBuilder.CreateTable(
                name: "BannedAddress",
                columns: table => new
                {
                    BannedAddressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedAddress", x => x.BannedAddressId);
                });

            migrationBuilder.CreateTable(
                name: "BannedDomain",
                columns: table => new
                {
                    BannedDomainId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DomainName = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedDomain", x => x.BannedDomainId);
                });
        }
    }
}
