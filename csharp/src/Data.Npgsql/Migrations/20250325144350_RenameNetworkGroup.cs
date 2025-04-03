using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class RenameNetworkGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "Networks");

            migrationBuilder.RenameColumn(
                name: "ApiSymbol",
                table: "TokenPrices",
                newName: "ExternalId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                comment: "Completed=0,Initiated=1,Failed=2",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Completed=0,Initiated=1");

            migrationBuilder.AddColumn<int>(
                name: "FeeType",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "EVM=0,Solana=1,Starknet=2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeType",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Networks");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "TokenPrices",
                newName: "ApiSymbol");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                comment: "Completed=0,Initiated=1",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Completed=0,Initiated=1,Failed=2");

            migrationBuilder.AddColumn<int>(
                name: "Group",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,SOLANA=8,STARKNET=9");
        }
    }
}
