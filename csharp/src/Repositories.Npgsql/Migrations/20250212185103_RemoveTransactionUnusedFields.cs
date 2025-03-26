using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations;

/// <inheritdoc />
public partial class RemoveTransactionUnusedFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "From",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "MaxConfirmations",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "To",
            table: "Transactions");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "From",
            table: "Transactions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MaxConfirmations",
            table: "Transactions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "To",
            table: "Transactions",
            type: "text",
            nullable: true);
    }
}
