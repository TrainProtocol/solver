using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class changeContractsOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractNetwork");

            migrationBuilder.AlterColumn<int>(
                name: "Group",
                table: "Networks",
                type: "integer",
                nullable: false,
                comment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,SOLANA=8,STARKNET=9",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,FUEL=8,IMMUTABLEX=9,LOOPRING=10,OSMOSIS=11,SOLANA=12,STARKNET=13,STARKNET_PARADEX=14,TON=15,TRON=16,ZKSYNC=17,BRINE=18,RHINOFI=19,APTOS=20,ZKSPACE=21,ZKSYNC_ERA_PAYMASTER=22");

            migrationBuilder.AddColumn<int>(
                name: "NetworkId",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_NetworkId",
                table: "Contracts",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Networks_NetworkId",
                table: "Contracts",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Networks_NetworkId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_NetworkId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "NetworkId",
                table: "Contracts");

            migrationBuilder.AlterColumn<int>(
                name: "Group",
                table: "Networks",
                type: "integer",
                nullable: false,
                comment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,FUEL=8,IMMUTABLEX=9,LOOPRING=10,OSMOSIS=11,SOLANA=12,STARKNET=13,STARKNET_PARADEX=14,TON=15,TRON=16,ZKSYNC=17,BRINE=18,RHINOFI=19,APTOS=20,ZKSPACE=21,ZKSYNC_ERA_PAYMASTER=22",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,SOLANA=8,STARKNET=9");

            migrationBuilder.CreateTable(
                name: "ContractNetwork",
                columns: table => new
                {
                    DeployedContractsId = table.Column<int>(type: "integer", nullable: false),
                    NetworksId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractNetwork", x => new { x.DeployedContractsId, x.NetworksId });
                    table.ForeignKey(
                        name: "FK_ContractNetwork_Contracts_DeployedContractsId",
                        column: x => x.DeployedContractsId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractNetwork_Networks_NetworksId",
                        column: x => x.NetworksId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractNetwork_NetworksId",
                table: "ContractNetwork",
                column: "NetworksId");
        }
    }
}
