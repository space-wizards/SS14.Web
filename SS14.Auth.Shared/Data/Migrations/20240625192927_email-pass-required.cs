using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class emailpassrequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireEmailChange",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePasswordChange",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireEmailChange",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RequirePasswordChange",
                table: "AspNetUsers");
        }
    }
}
