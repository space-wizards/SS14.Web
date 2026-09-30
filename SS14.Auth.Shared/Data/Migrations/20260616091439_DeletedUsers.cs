using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class DeletedUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletedUserIds",
                columns: table => new
                {
                    SpaceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedUserIds", x => x.SpaceUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedUserIds_DeletedOn",
                table: "DeletedUserIds",
                column: "DeletedOn");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedUserIds");
        }
    }
}
