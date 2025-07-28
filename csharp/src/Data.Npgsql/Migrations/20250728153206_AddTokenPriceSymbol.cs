using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPriceSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "TokenPrices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPrices_Symbol",
                table: "TokenPrices",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenPrices_Symbol",
                table: "TokenPrices");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "TokenPrices");
        }
    }
}
