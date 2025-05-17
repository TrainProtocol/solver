using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class addFuelNetworkType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Networks",
                type: "integer",
                nullable: false,
                comment: "EVM=0,Solana=1,Starknet=2,Fuel=3",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "EVM=0,Solana=1,Starknet=2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Networks",
                type: "integer",
                nullable: false,
                comment: "EVM=0,Solana=1,Starknet=2",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "EVM=0,Solana=1,Starknet=2,Fuel=3");
        }
    }
}
