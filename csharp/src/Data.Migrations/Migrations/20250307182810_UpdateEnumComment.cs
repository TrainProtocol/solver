using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEnumComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Contracts",
                type: "integer",
                nullable: false,
                comment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,EvmMultiCallContract=3",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,ZKSPaymasterContract=3,EvmMultiCallContract=4,EvmOracleContract=5,WatchdogContractAddress=6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Contracts",
                type: "integer",
                nullable: false,
                comment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,ZKSPaymasterContract=3,EvmMultiCallContract=4,EvmOracleContract=5,WatchdogContractAddress=6",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,EvmMultiCallContract=3");
        }
    }
}
