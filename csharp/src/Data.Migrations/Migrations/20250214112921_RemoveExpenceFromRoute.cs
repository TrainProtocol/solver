using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations;

/// <inheritdoc />
public partial class RemoveExpenceFromRoute : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Expenses_Routes_RouteId",
            table: "Expenses");

        migrationBuilder.DropIndex(
            name: "IX_Expenses_RouteId",
            table: "Expenses");

        migrationBuilder.DropColumn(
            name: "RouteId",
            table: "Expenses");

        migrationBuilder.AlterColumn<int>(
            name: "Status",
            table: "Transactions",
            type: "integer",
            nullable: false,
            comment: "Completed=0,Initiated=1",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Completed=0,Initiated=1,Pending=2");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Status",
            table: "Transactions",
            type: "integer",
            nullable: false,
            comment: "Completed=0,Initiated=1,Pending=2",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Completed=0,Initiated=1");

        migrationBuilder.AddColumn<int>(
            name: "RouteId",
            table: "Expenses",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_RouteId",
            table: "Expenses",
            column: "RouteId");

        migrationBuilder.AddForeignKey(
            name: "FK_Expenses_Routes_RouteId",
            table: "Expenses",
            column: "RouteId",
            principalTable: "Routes",
            principalColumn: "Id");
    }
}
