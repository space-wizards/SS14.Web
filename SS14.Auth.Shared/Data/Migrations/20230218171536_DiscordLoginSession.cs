using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class DiscordLoginSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordLoginSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Expires = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordLoginSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordLoginSessions_AspNetUsers_SpaceUserId",
                        column: x => x.SpaceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordLoginSessions_SpaceUserId",
                table: "DiscordLoginSessions",
                column: "SpaceUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordLoginSessions");
        }
    }
}
