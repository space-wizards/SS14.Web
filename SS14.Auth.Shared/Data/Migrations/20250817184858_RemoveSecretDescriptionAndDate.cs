using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSecretDescriptionAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecretDescription",
                table: "OpenIddictApplications");

            migrationBuilder.DropColumn(
                name: "SecretCreationDate",
                table: "OpenIddictApplications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecretDescription",
                table: "OpenIddictApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretCreationDate",
                table: "OpenIddictApplications",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
