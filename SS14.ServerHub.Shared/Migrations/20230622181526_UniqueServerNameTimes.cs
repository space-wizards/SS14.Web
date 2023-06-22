using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class UniqueServerNameTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSeen",
                table: "UniqueServerName",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "UniqueServerName",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                DELETE FROM "UniqueServerName";

                WITH advertised_groups AS (
                    SELECT
                        "AdvertisedServerId" id,
                        "StatusData"->>'name' name,
                        MIN("Time") min,
                        MAX("Time") max
                    FROM
                        "ServerStatusArchive"
                    GROUP BY
                        "AdvertisedServerId", "StatusData"->>'name'
                )
                INSERT INTO
                    "UniqueServerName" ("AdvertisedServerId", "Name", "FirstSeen", "LastSeen")
                SELECT
                    *
                FROM
                    advertised_groups;
                """
            );
            
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION process_log_unique_server_name() RETURNS TRIGGER AS $unique_server_name$
                    BEGIN
                        INSERT INTO
                            "UniqueServerName" ("AdvertisedServerId", "Name", "FirstSeen", "LastSeen")
                        VALUES
                            (NEW."AdvertisedServerId", NEW."StatusData"->>'name', NOW(), NOW())
                        ON CONFLICT ON CONSTRAINT "PK_UniqueServerName" DO UPDATE SET
                            "LastSeen" = NOW();

                        RETURN NULL;
                    END;
                $unique_server_name$ LANGUAGE plpgsql;
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSeen",
                table: "UniqueServerName");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "UniqueServerName");

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
                """);
        }
    }
}
