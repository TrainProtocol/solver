using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations;

/// <inheritdoc />
public partial class DeleteUnusedTxType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Type",
            table: "Transactions",
            type: "integer",
            nullable: false,
            comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7");

        migrationBuilder.AlterColumn<int>(
            name: "TransactionType",
            table: "Expenses",
            type: "integer",
            nullable: false,
            comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Type",
            table: "Transactions",
            type: "integer",
            nullable: false,
            comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6");

        migrationBuilder.AlterColumn<int>(
            name: "TransactionType",
            table: "Expenses",
            type: "integer",
            nullable: false,
            comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7",
            oldClrType: typeof(int),
            oldType: "integer",
            oldComment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6");
    }
}
