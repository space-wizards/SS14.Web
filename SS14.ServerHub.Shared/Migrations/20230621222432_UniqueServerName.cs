using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class UniqueServerName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UniqueServerName",
                columns: table => new
                {
                    AdvertisedServerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniqueServerName", x => new { x.AdvertisedServerId, x.Name });
                    table.ForeignKey(
                        name: "FK_UniqueServerName_AdvertisedServer_AdvertisedServerId",
                        column: x => x.AdvertisedServerId,
                        principalTable: "AdvertisedServer",
                        principalColumn: "AdvertisedServerId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Retroactively backfill UniqueServerName table.
            migrationBuilder.Sql("""
                INSERT INTO
                    "UniqueServerName" ("AdvertisedServerId", "Name")
                SELECT DISTINCT
                    "AdvertisedServerId", "StatusData"->>'name' FROM "ServerStatusArchive";
                """);
            
            // Create trigger to fill it automatically from now on.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION process_log_unique_server_name() RETURNS TRIGGER AS $unique_server_name$
                    BEGIN
                        INSERT INTO
                            "UniqueServerName" ("AdvertisedServerId", "Name")
                        VALUES
                            (NEW."AdvertisedServerId", NEW."StatusData"->>'name')
                        ON CONFLICT DO NOTHING;

                        RETURN NULL;
                    END;
                $unique_server_name$ LANGUAGE plpgsql;

                CREATE TRIGGER "LogUniqueServerName"
                AFTER INSERT OR UPDATE OR DELETE ON "AdvertisedServer"
                    FOR EACH ROW EXECUTE FUNCTION process_log_unique_server_name();
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UniqueServerName");
            
            migrationBuilder.Sql("""
                DROP TRIGGER "LogUniqueServerName" ON "AdvertisedServer";

                DROP FUNCTION process_log_unique_server_name;
                """);
        }
    }
}
