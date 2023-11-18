using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class InfoMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedCommunityInfoMatch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "jsonpath", nullable: false),
                    Field = table.Column<int>(type: "integer", nullable: false),
                    TrackedCommunityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedCommunityInfoMatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedCommunityInfoMatch_TrackedCommunity_TrackedCommunity~",
                        column: x => x.TrackedCommunityId,
                        principalTable: "TrackedCommunity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedCommunityInfoMatch_TrackedCommunityId",
                table: "TrackedCommunityInfoMatch",
                column: "TrackedCommunityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedCommunityInfoMatch");
        }
    }
}
