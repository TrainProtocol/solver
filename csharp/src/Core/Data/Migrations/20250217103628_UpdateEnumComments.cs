using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations;

/// <inheritdoc />
public partial class UpdateEnumComments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Status",
            table: "Routes",
            type: "integer",
            nullable: false,
            comment: "Active=0,Inactive=1,Archived=2",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Active=0,Archived=1,DelayedWithdrawal=2,DelayedDeposit=3,DailyLimitReached=4,UnderMaintenance=5");

        migrationBuilder.AlterColumn<int>(
            name: "Type",
            table: "Nodes",
            type: "integer",
            nullable: false,
            comment: "Primary=0,DepositTracking=1,Public=2,Secondary=3",
            oldClrType: typeof(int),
            oldType: "integer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Status",
            table: "Routes",
            type: "integer",
            nullable: false,
            comment: "Active=0,Archived=1,DelayedWithdrawal=2,DelayedDeposit=3,DailyLimitReached=4,UnderMaintenance=5",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Active=0,Inactive=1,Archived=2");

        migrationBuilder.AlterColumn<int>(
            name: "Type",
            table: "Nodes",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Primary=0,DepositTracking=1,Public=2,Secondary=3");
    }
}
