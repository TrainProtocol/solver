using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations;

/// <inheritdoc />
public partial class AddIndexesInSwaps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Swaps_CreatedDate",
            table: "Swaps",
            column: "CreatedDate");

        migrationBuilder.CreateIndex(
            name: "IX_Swaps_DestinationAddress",
            table: "Swaps",
            column: "DestinationAddress");

        migrationBuilder.CreateIndex(
            name: "IX_Swaps_SourceAddress",
            table: "Swaps",
            column: "SourceAddress");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Swaps_CreatedDate",
            table: "Swaps");

        migrationBuilder.DropIndex(
            name: "IX_Swaps_DestinationAddress",
            table: "Swaps");

        migrationBuilder.DropIndex(
            name: "IX_Swaps_SourceAddress",
            table: "Swaps");
    }
}
