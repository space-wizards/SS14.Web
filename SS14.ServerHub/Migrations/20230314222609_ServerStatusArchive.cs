using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.ServerHub.Migrations
{
    public partial class ServerStatusArchive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<IPAddress>(
                name: "AdvertiserAddress",
                table: "AdvertisedServer",
                type: "inet",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServerStatusArchive",
                columns: table => new
                {
                    AdvertisedServerId = table.Column<int>(type: "integer", nullable: false),
                    ServerStatusArchiveId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusData = table.Column<byte[]>(type: "jsonb", nullable: false),
                    AdvertiserAddress = table.Column<IPAddress>(type: "inet", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStatusArchive", x => new { x.AdvertisedServerId, x.ServerStatusArchiveId });
                    table.ForeignKey(
                        name: "FK_ServerStatusArchive_AdvertisedServer_AdvertisedServerId",
                        column: x => x.AdvertisedServerId,
                        principalTable: "AdvertisedServer",
                        principalColumn: "AdvertisedServerId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerStatusArchive");

            migrationBuilder.DropColumn(
                name: "AdvertiserAddress",
                table: "AdvertisedServer");
        }
    }
}
