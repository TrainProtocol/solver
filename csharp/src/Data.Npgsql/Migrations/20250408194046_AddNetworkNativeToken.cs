using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkNativeToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NativeTokenId",
                table: "Networks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_NativeTokenId",
                table: "Networks",
                column: "NativeTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Tokens_NativeTokenId",
                table: "Networks",
                column: "NativeTokenId",
                principalTable: "Tokens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Tokens_NativeTokenId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_NativeTokenId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "NativeTokenId",
                table: "Networks");
        }
    }
}
