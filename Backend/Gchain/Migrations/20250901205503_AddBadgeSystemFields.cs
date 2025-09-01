using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gchain.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeSystemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "UserBadges",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Badges",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Badges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiredValue",
                table: "Badges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Badges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Badges",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "UserBadges");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "RequiredValue",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Badges");
        }
    }
}
