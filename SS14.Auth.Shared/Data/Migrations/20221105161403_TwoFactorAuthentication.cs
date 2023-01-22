using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class TwoFactorAuthentication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "AspNetUsers");
        }
    }
}
