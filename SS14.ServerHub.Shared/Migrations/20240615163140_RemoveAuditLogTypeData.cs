using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class RemoveAuditLogTypeData : Migration
    {
        // There was a bug where the "Type" field of the HubAuditEntry type was getting saved into the data too.
        // This cleans those out as that's silly.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"HubAudit\" SET \"Data\" = \"Data\" #- '{\"Type\"}';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
