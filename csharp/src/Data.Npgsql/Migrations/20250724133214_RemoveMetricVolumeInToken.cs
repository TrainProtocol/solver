using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMetricVolumeInToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Profit",
                table: "SwapMetrics");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "SwapMetrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Amount",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Profit",
                table: "SwapMetrics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "SwapMetrics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
