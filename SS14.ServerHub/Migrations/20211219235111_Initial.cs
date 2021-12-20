using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.ServerHub.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvertisedServer",
                columns: table => new
                {
                    AdvertisedServerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Secret = table.Column<byte[]>(type: "bytea", nullable: false),
                    Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisedServer", x => x.AdvertisedServerId);
                    table.CheckConstraint("CK_AdvertisedServer_AddressSs14Uri", "\"Address\" LIKE 'ss14://%' OR \"Address\" LIKE 'ss14s://%'");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisedServer_Address",
                table: "AdvertisedServer",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisedServer_Secret",
                table: "AdvertisedServer",
                column: "Secret",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvertisedServer");
        }
    }
}
