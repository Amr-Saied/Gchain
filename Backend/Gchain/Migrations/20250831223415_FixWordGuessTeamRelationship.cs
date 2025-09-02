using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gchain.Migrations
{
    /// <inheritdoc />
    public partial class FixWordGuessTeamRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses");

            migrationBuilder.AddForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses");

            migrationBuilder.AddForeignKey(
                name: "FK_WordGuesses_Teams_TeamId",
                table: "WordGuesses",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
