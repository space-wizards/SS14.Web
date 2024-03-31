using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    public partial class InferredTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "InferredTags",
                table: "ServerStatusArchive",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "InferredTags",
                table: "AdvertisedServer",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InferredTags",
                table: "ServerStatusArchive");

            migrationBuilder.DropColumn(
                name: "InferredTags",
                table: "AdvertisedServer");
        }
    }
}
