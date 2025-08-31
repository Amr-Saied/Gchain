using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gchain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWordGuessAndRoundResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GuessedAt",
                table: "WordGuesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "WordGuesses",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AlterColumn<int>(
                name: "WinningTeamId",
                table: "RoundResults",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "RoundResults",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RoundResults",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WordGuesses_TeamId",
                table: "WordGuesses",
                column: "TeamId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses"
            );

            migrationBuilder.DropIndex(name: "IX_WordGuesses_TeamId", table: "WordGuesses");

            migrationBuilder.DropColumn(name: "GuessedAt", table: "WordGuesses");

            migrationBuilder.DropColumn(name: "TeamId", table: "WordGuesses");

            migrationBuilder.DropColumn(name: "Color", table: "Teams");

            migrationBuilder.DropColumn(name: "CompletedAt", table: "RoundResults");

            migrationBuilder.DropColumn(name: "Notes", table: "RoundResults");

            migrationBuilder.AlterColumn<int>(
                name: "WinningTeamId",
                table: "RoundResults",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true
            );
        }
    }
}
