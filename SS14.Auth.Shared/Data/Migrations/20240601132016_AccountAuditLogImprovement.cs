using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class AccountAuditLogImprovement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Actor",
                table: "AccountLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<IPAddress>(
                name: "ActorAddress",
                table: "AccountLogs",
                type: "inet",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"AccountLogs\" SET \"Actor\" = (\"Data\"->>'Actor')::uuid WHERE \"Data\" ? 'Actor';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actor",
                table: "AccountLogs");

            migrationBuilder.DropColumn(
                name: "ActorAddress",
                table: "AccountLogs");
        }
    }
}
