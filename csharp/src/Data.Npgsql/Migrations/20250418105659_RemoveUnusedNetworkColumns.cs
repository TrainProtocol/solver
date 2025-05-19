using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedNetworkColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedGasPriceInGwei",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "GasLimitPercentageIncrease",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "IsTestnet",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "ReplacementFeePercentage",
                table: "Networks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FixedGasPriceInGwei",
                table: "Networks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GasLimitPercentageIncrease",
                table: "Networks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "Networks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestnet",
                table: "Networks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReplacementFeePercentage",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
