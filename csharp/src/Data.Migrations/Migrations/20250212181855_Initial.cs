using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

        migrationBuilder.CreateTable(
            name: "Apps",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false),
                ApiKey = table.Column<string>(type: "text", nullable: true),
                SandboxApiKey = table.Column<string>(type: "text", nullable: true),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Apps", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Contracts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Type = table.Column<int>(type: "integer", nullable: false, comment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,ZKSPaymasterContract=3,EvmMultiCallContract=4,EvmOracleContract=5,WatchdogContractAddress=6"),
                Address = table.Column<string>(type: "text", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contracts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Networks",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: false),
                Group = table.Column<int>(type: "integer", nullable: false, comment: "EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,FUEL=8,IMMUTABLEX=9,LOOPRING=10,OSMOSIS=11,SOLANA=12,STARKNET=13,STARKNET_PARADEX=14,TON=15,TRON=16,ZKSYNC=17,BRINE=18,RHINOFI=19,APTOS=20,ZKSPACE=21,ZKSYNC_ERA_PAYMASTER=22"),
                ChainId = table.Column<string>(type: "text", nullable: true),
                FeePercentageIncrease = table.Column<int>(type: "integer", nullable: false),
                GasLimitPercentageIncrease = table.Column<int>(type: "integer", nullable: true),
                FixedGasPriceInGwei = table.Column<string>(type: "text", nullable: true),
                TransactionExplorerTemplate = table.Column<string>(type: "text", nullable: false),
                AccountExplorerTemplate = table.Column<string>(type: "text", nullable: false),
                IsTestnet = table.Column<bool>(type: "boolean", nullable: false),
                ReplacementFeePercentage = table.Column<int>(type: "integer", nullable: false),
                IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                Logo = table.Column<string>(type: "text", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Networks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ServiceFees",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SourceNetwork = table.Column<string>(type: "text", nullable: true),
                DestinationNetwork = table.Column<string>(type: "text", nullable: true),
                SourceAsset = table.Column<string>(type: "text", nullable: true),
                DestinationAsset = table.Column<string>(type: "text", nullable: true),
                FeeInUsd = table.Column<decimal>(type: "numeric", nullable: false),
                FeePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServiceFees", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TokenPrices",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PriceInUsd = table.Column<decimal>(type: "numeric", nullable: false),
                ApiSymbol = table.Column<string>(type: "text", nullable: true),
                LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TokenPrices", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Deployments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                NetworkName = table.Column<string>(type: "text", nullable: false),
                AppId = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Deployments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Deployments_Apps_AppId",
                    column: x => x.AppId,
                    principalTable: "Apps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

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

        migrationBuilder.CreateTable(
            name: "ManagedAccounts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Address = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false, comment: "LP=0,Charging=1"),
                NetworkId = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ManagedAccounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ManagedAccounts_Networks_NetworkId",
                    column: x => x.NetworkId,
                    principalTable: "Networks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Nodes",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Url = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                NetworkId = table.Column<int>(type: "integer", nullable: false),
                TraceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                Priority = table.Column<double>(type: "double precision", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Nodes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Nodes_Networks_NetworkId",
                    column: x => x.NetworkId,
                    principalTable: "Networks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReservedNonces",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Nonce = table.Column<string>(type: "text", nullable: false),
                ReferenceId = table.Column<string>(type: "text", nullable: false),
                NetworkId = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReservedNonces", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReservedNonces_Networks_NetworkId",
                    column: x => x.NetworkId,
                    principalTable: "Networks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tokens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Asset = table.Column<string>(type: "text", nullable: false),
                TokenContract = table.Column<string>(type: "text", nullable: true),
                IsNative = table.Column<bool>(type: "boolean", nullable: false),
                Precision = table.Column<int>(type: "integer", nullable: false),
                Decimals = table.Column<int>(type: "integer", nullable: false),
                NetworkId = table.Column<int>(type: "integer", nullable: false),
                Logo = table.Column<string>(type: "text", nullable: false),
                TokenPriceId = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tokens_Networks_NetworkId",
                    column: x => x.NetworkId,
                    principalTable: "Networks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Tokens_TokenPrices_TokenPriceId",
                    column: x => x.TokenPriceId,
                    principalTable: "TokenPrices",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Routes",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaxAmountInSource = table.Column<decimal>(type: "numeric", nullable: false),
                SourceTokenId = table.Column<int>(type: "integer", nullable: false),
                DestinationTokenId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false, comment: "Active=0,Archived=1,DelayedWithdrawal=2,DelayedDeposit=3,DailyLimitReached=4,UnderMaintenance=5"),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Routes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Routes_Tokens_DestinationTokenId",
                    column: x => x.DestinationTokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Routes_Tokens_SourceTokenId",
                    column: x => x.SourceTokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Swaps",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                SourceTokenId = table.Column<int>(type: "integer", nullable: false),
                DestinationTokenId = table.Column<int>(type: "integer", nullable: false),
                SourceAddress = table.Column<string>(type: "text", nullable: false),
                DestinationAddress = table.Column<string>(type: "text", nullable: false),
                SourceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                Hashlock = table.Column<string>(type: "text", nullable: false),
                DestinationAmount = table.Column<decimal>(type: "numeric", nullable: false),
                FeeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Swaps", x => x.Id);
                table.ForeignKey(
                    name: "FK_Swaps_Tokens_DestinationTokenId",
                    column: x => x.DestinationTokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Swaps_Tokens_SourceTokenId",
                    column: x => x.SourceTokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Expenses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FeeTokenId = table.Column<int>(type: "integer", nullable: false),
                TokenId = table.Column<int>(type: "integer", nullable: false),
                LastFeeValues = table.Column<decimal[]>(type: "numeric[]", nullable: false),
                TransactionType = table.Column<int>(type: "integer", nullable: false, comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7"),
                RouteId = table.Column<int>(type: "integer", nullable: true),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Expenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Expenses_Routes_RouteId",
                    column: x => x.RouteId,
                    principalTable: "Routes",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Expenses_Tokens_FeeTokenId",
                    column: x => x.FeeTokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Expenses_Tokens_TokenId",
                    column: x => x.TokenId,
                    principalTable: "Tokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                From = table.Column<string>(type: "text", nullable: true),
                To = table.Column<string>(type: "text", nullable: true),
                TransactionId = table.Column<string>(type: "text", nullable: true),
                Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                NetworkName = table.Column<string>(type: "text", nullable: false),
                Confirmations = table.Column<int>(type: "integer", nullable: false),
                MaxConfirmations = table.Column<int>(type: "integer", nullable: false),
                Asset = table.Column<string>(type: "text", nullable: true),
                Amount = table.Column<decimal>(type: "numeric", nullable: false),
                UsdPrice = table.Column<decimal>(type: "numeric", nullable: false),
                FeeAsset = table.Column<string>(type: "text", nullable: true),
                FeeAmount = table.Column<decimal>(type: "numeric", nullable: true),
                FeeUsdPrice = table.Column<decimal>(type: "numeric", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false, comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7"),
                Status = table.Column<int>(type: "integer", nullable: false, comment: "Completed=0,Initiated=1,Pending=2"),
                SwapId = table.Column<string>(type: "text", nullable: true),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Swaps_SwapId",
                    column: x => x.SwapId,
                    principalTable: "Swaps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Apps_ApiKey",
            table: "Apps",
            column: "ApiKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Apps_SandboxApiKey",
            table: "Apps",
            column: "SandboxApiKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContractNetwork_NetworksId",
            table: "ContractNetwork",
            column: "NetworksId");

        migrationBuilder.CreateIndex(
            name: "IX_Deployments_AppId",
            table: "Deployments",
            column: "AppId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_FeeTokenId",
            table: "Expenses",
            column: "FeeTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_RouteId",
            table: "Expenses",
            column: "RouteId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_TokenId_FeeTokenId_TransactionType",
            table: "Expenses",
            columns: new[] { "TokenId", "FeeTokenId", "TransactionType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ManagedAccounts_Address",
            table: "ManagedAccounts",
            column: "Address");

        migrationBuilder.CreateIndex(
            name: "IX_ManagedAccounts_NetworkId",
            table: "ManagedAccounts",
            column: "NetworkId");

        migrationBuilder.CreateIndex(
            name: "IX_Networks_Name",
            table: "Networks",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Nodes_NetworkId",
            table: "Nodes",
            column: "NetworkId");

        migrationBuilder.CreateIndex(
            name: "IX_Nodes_Type_NetworkId",
            table: "Nodes",
            columns: new[] { "Type", "NetworkId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ReservedNonces_NetworkId_ReferenceId",
            table: "ReservedNonces",
            columns: new[] { "NetworkId", "ReferenceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Routes_DestinationTokenId",
            table: "Routes",
            column: "DestinationTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_Routes_SourceTokenId_DestinationTokenId",
            table: "Routes",
            columns: new[] { "SourceTokenId", "DestinationTokenId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Swaps_DestinationTokenId",
            table: "Swaps",
            column: "DestinationTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_Swaps_SourceTokenId",
            table: "Swaps",
            column: "SourceTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_NetworkId_Asset",
            table: "Tokens",
            columns: new[] { "NetworkId", "Asset" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_TokenPriceId",
            table: "Tokens",
            column: "TokenPriceId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Status",
            table: "Transactions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_SwapId",
            table: "Transactions",
            column: "SwapId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_TransactionId",
            table: "Transactions",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_TransactionId_NetworkName",
            table: "Transactions",
            columns: new[] { "TransactionId", "NetworkName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Type",
            table: "Transactions",
            column: "Type");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContractNetwork");

        migrationBuilder.DropTable(
            name: "Deployments");

        migrationBuilder.DropTable(
            name: "Expenses");

        migrationBuilder.DropTable(
            name: "ManagedAccounts");

        migrationBuilder.DropTable(
            name: "Nodes");

        migrationBuilder.DropTable(
            name: "ReservedNonces");

        migrationBuilder.DropTable(
            name: "ServiceFees");

        migrationBuilder.DropTable(
            name: "Transactions");

        migrationBuilder.DropTable(
            name: "Contracts");

        migrationBuilder.DropTable(
            name: "Apps");

        migrationBuilder.DropTable(
            name: "Routes");

        migrationBuilder.DropTable(
            name: "Swaps");

        migrationBuilder.DropTable(
            name: "Tokens");

        migrationBuilder.DropTable(
            name: "Networks");

        migrationBuilder.DropTable(
            name: "TokenPrices");
    }
}
