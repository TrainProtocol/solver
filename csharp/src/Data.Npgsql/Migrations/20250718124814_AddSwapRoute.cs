using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Swaps_Tokens_DestinationTokenId",
                table: "Swaps");

            migrationBuilder.DropForeignKey(
                name: "FK_Swaps_Tokens_SourceTokenId",
                table: "Swaps");

            migrationBuilder.DropIndex(
                name: "IX_Swaps_DestinationTokenId",
                table: "Swaps");

            migrationBuilder.DropColumn(
                name: "DestinationTokenId",
                table: "Swaps");

            migrationBuilder.RenameColumn(
                name: "SourceTokenId",
                table: "Swaps",
                newName: "RouteId");

            migrationBuilder.RenameIndex(
                name: "IX_Swaps_SourceTokenId",
                table: "Swaps",
                newName: "IX_Swaps_RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Swaps_Routes_RouteId",
                table: "Swaps",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Swaps_Routes_RouteId",
                table: "Swaps");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "Swaps",
                newName: "SourceTokenId");

            migrationBuilder.RenameIndex(
                name: "IX_Swaps_RouteId",
                table: "Swaps",
                newName: "IX_Swaps_SourceTokenId");

            migrationBuilder.AddColumn<int>(
                name: "DestinationTokenId",
                table: "Swaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_DestinationTokenId",
                table: "Swaps",
                column: "DestinationTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Swaps_Tokens_DestinationTokenId",
                table: "Swaps",
                column: "DestinationTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Swaps_Tokens_SourceTokenId",
                table: "Swaps",
                column: "SourceTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
