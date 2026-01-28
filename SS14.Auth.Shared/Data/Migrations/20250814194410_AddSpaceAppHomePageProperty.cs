using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceAppHomePageProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOAuthClients");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "OpenIddictApplications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "OpenIddictApplications");

            migrationBuilder.CreateTable(
                name: "UserOAuthClients",
                columns: table => new
                {
                    UserOAuthClientId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpaceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOAuthClients", x => x.UserOAuthClientId);
                    table.ForeignKey(
                        name: "FK_UserOAuthClients_AspNetUsers_SpaceUserId",
                        column: x => x.SpaceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOAuthClients_ClientId",
                table: "UserOAuthClients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOAuthClients_SpaceUserId",
                table: "UserOAuthClients",
                column: "SpaceUserId");
        }
    }
}
