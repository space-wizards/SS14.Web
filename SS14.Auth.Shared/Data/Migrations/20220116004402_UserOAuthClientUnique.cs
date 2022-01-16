using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class UserOAuthClientUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOAuthClients_ClientId",
                table: "UserOAuthClients");

            migrationBuilder.CreateIndex(
                name: "IX_UserOAuthClients_ClientId",
                table: "UserOAuthClients",
                column: "ClientId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOAuthClients_ClientId",
                table: "UserOAuthClients");

            migrationBuilder.CreateIndex(
                name: "IX_UserOAuthClients_ClientId",
                table: "UserOAuthClients",
                column: "ClientId");
        }
    }
}
