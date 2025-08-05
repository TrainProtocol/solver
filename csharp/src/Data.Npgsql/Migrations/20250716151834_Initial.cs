using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    NetworkType = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeeTokenId = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    LastFeeValues = table.Column<string[]>(type: "text[]", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false, comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6"),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "EVM=0,Solana=1,Starknet=2,Fuel=3"),
                    FeeType = table.Column<int>(type: "integer", nullable: false),
                    ChainId = table.Column<string>(type: "text", nullable: false),
                    FeePercentageIncrease = table.Column<int>(type: "integer", nullable: false),
                    HTLCNativeContractAddress = table.Column<string>(type: "text", nullable: false),
                    HTLCTokenContractAddress = table.Column<string>(type: "text", nullable: false),
                    NativeTokenId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    NetworkId = table.Column<int>(type: "integer", nullable: false),
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
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Asset = table.Column<string>(type: "text", nullable: false),
                    TokenContract = table.Column<string>(type: "text", nullable: true),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    NetworkId = table.Column<int>(type: "integer", nullable: false),
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
                    SourceWalletId = table.Column<int>(type: "integer", nullable: false),
                    DestinationWalletId = table.Column<int>(type: "integer", nullable: false),
                    RateProviderId = table.Column<int>(type: "integer", nullable: false),
                    ServiceFeeId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "Active=0,Inactive=1,Archived=2"),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_RateProviders_RateProviderId",
                        column: x => x.RateProviderId,
                        principalTable: "RateProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_ServiceFees_ServiceFeeId",
                        column: x => x.ServiceFeeId,
                        principalTable: "ServiceFees",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_Routes_Wallets_DestinationWalletId",
                        column: x => x.DestinationWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_Wallets_SourceWalletId",
                        column: x => x.SourceWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Swaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommitId = table.Column<string>(type: "text", nullable: false),
                    SourceTokenId = table.Column<int>(type: "integer", nullable: false),
                    DestinationTokenId = table.Column<int>(type: "integer", nullable: false),
                    SourceAddress = table.Column<string>(type: "text", nullable: false),
                    DestinationAddress = table.Column<string>(type: "text", nullable: false),
                    Hashlock = table.Column<string>(type: "text", nullable: false),
                    SourceAmount = table.Column<string>(type: "text", nullable: false),
                    DestinationAmount = table.Column<string>(type: "text", nullable: false),
                    FeeAmount = table.Column<string>(type: "text", nullable: false),
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
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionHash = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NetworkName = table.Column<string>(type: "text", nullable: false),
                    Confirmations = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    FeeTokenId = table.Column<int>(type: "integer", nullable: false),
                    FeeAmount = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6"),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "Completed=0,Initiated=1,Failed=2"),
                    SwapId = table.Column<int>(type: "integer", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Transactions_Tokens_FeeTokenId",
                        column: x => x.FeeTokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_FeeTokenId",
                table: "Expenses",
                column: "FeeTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TokenId_FeeTokenId_TransactionType",
                table: "Expenses",
                columns: new[] { "TokenId", "FeeTokenId", "TransactionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_Name",
                table: "Networks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_NativeTokenId",
                table: "Networks",
                column: "NativeTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_NetworkId",
                table: "Nodes",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_RateProviders_Name",
                table: "RateProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DestinationTokenId",
                table: "Routes",
                column: "DestinationTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DestinationWalletId",
                table: "Routes",
                column: "DestinationWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_RateProviderId",
                table: "Routes",
                column: "RateProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ServiceFeeId",
                table: "Routes",
                column: "ServiceFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_SourceTokenId_DestinationTokenId",
                table: "Routes",
                columns: new[] { "SourceTokenId", "DestinationTokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_SourceWalletId",
                table: "Routes",
                column: "SourceWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_CommitId",
                table: "Swaps",
                column: "CommitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_CreatedDate",
                table: "Swaps",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_DestinationAddress",
                table: "Swaps",
                column: "DestinationAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_DestinationTokenId",
                table: "Swaps",
                column: "DestinationTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_SourceAddress",
                table: "Swaps",
                column: "SourceAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Swaps_SourceTokenId",
                table: "Swaps",
                column: "SourceTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Asset",
                table: "Tokens",
                column: "Asset");

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
                name: "IX_Transactions_FeeTokenId",
                table: "Transactions",
                column: "FeeTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SwapId",
                table: "Transactions",
                column: "SwapId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TokenId",
                table: "Transactions",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionHash",
                table: "Transactions",
                column: "TransactionHash");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionHash_NetworkName",
                table: "Transactions",
                columns: new[] { "TransactionHash", "NetworkName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Type",
                table: "Transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Address_NetworkType",
                table: "Wallets",
                columns: new[] { "Address", "NetworkType" });

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Tokens_FeeTokenId",
                table: "Expenses",
                column: "FeeTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Tokens_TokenId",
                table: "Expenses",
                column: "TokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Tokens_NativeTokenId",
                table: "Networks",
                column: "NativeTokenId",
                principalTable: "Tokens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Tokens_NativeTokenId",
                table: "Networks");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "RateProviders");

            migrationBuilder.DropTable(
                name: "ServiceFees");

            migrationBuilder.DropTable(
                name: "Wallets");

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
}
