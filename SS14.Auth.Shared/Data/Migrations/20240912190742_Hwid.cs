using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class Hwid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HwidId",
                table: "AuthHashes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Hwids",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeCode = table.Column<int>(type: "integer", nullable: false),
                    ClientData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Value = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hwids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HwidUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HwidId = table.Column<long>(type: "bigint", nullable: false),
                    SpaceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HwidUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HwidUsers_AspNetUsers_SpaceUserId",
                        column: x => x.SpaceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HwidUsers_Hwids_HwidId",
                        column: x => x.HwidId,
                        principalTable: "Hwids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthHashes_HwidId",
                table: "AuthHashes",
                column: "HwidId");

            migrationBuilder.CreateIndex(
                name: "IX_Hwids_ClientData",
                table: "Hwids",
                column: "ClientData",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HwidUsers_HwidId_SpaceUserId",
                table: "HwidUsers",
                columns: new[] { "HwidId", "SpaceUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HwidUsers_SpaceUserId",
                table: "HwidUsers",
                column: "SpaceUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthHashes_Hwids_HwidId",
                table: "AuthHashes",
                column: "HwidId",
                principalTable: "Hwids",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthHashes_Hwids_HwidId",
                table: "AuthHashes");

            migrationBuilder.DropTable(
                name: "HwidUsers");

            migrationBuilder.DropTable(
                name: "Hwids");

            migrationBuilder.DropIndex(
                name: "IX_AuthHashes_HwidId",
                table: "AuthHashes");

            migrationBuilder.DropColumn(
                name: "HwidId",
                table: "AuthHashes");
        }
    }
}
