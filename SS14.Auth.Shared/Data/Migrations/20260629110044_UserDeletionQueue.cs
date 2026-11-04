using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class UserDeletionQueue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDeletionQueue",
                columns: table => new
                {
                    SpaceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueuedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeletionQueue", x => x.SpaceUserId);
                    table.ForeignKey(
                        name: "FK_UserDeletionQueue_AspNetUsers_SpaceUserId",
                        column: x => x.SpaceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDeletionQueue");
        }
    }
}
