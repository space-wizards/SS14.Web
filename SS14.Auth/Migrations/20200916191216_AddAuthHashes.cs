using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SS14.Auth.Migrations
{
    public partial class AddAuthHashes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthHashes",
                columns: table => new
                {
                    AuthHashId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpaceUserId = table.Column<Guid>(nullable: false),
                    Expires = table.Column<DateTimeOffset>(nullable: false),
                    Hash = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthHashes", x => x.AuthHashId);
                    table.ForeignKey(
                        name: "FK_AuthHashes_AspNetUsers_SpaceUserId",
                        column: x => x.SpaceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthHashes_SpaceUserId",
                table: "AuthHashes",
                column: "SpaceUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthHashes_Hash_SpaceUserId",
                table: "AuthHashes",
                columns: new[] { "Hash", "SpaceUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthHashes");
        }
    }
}
