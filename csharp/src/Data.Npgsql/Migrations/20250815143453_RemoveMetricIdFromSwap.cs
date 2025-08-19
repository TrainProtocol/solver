using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMetricIdFromSwap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SwapMetrics_SwapId",
                table: "SwapMetrics");

            migrationBuilder.DropColumn(
                name: "MetricId",
                table: "Swaps");

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_SwapId",
                table: "SwapMetrics",
                column: "SwapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SwapMetrics_SwapId",
                table: "SwapMetrics");

            migrationBuilder.AddColumn<int>(
                name: "MetricId",
                table: "Swaps",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_SwapId",
                table: "SwapMetrics",
                column: "SwapId",
                unique: true);
        }
    }
}
