using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class RemoveAccountLogTypeData : Migration
    {
        // There was a bug where the "Type" field of the AccountLogEntry type was getting saved into the data too.
        // This cleans those out as that's silly.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"AccountLog\" SET \"Data\" = \"Data\" #- '{\"Type\"}';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
