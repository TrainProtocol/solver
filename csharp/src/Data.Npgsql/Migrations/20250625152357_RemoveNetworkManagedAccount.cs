using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNetworkManagedAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagedAccounts_Networks_NetworkId",
                table: "ManagedAccounts");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ManagedAccounts_Address",
                table: "ManagedAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ManagedAccounts_NetworkId",
                table: "ManagedAccounts");

            migrationBuilder.DropColumn(
                name: "IsNative",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Precision",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "TraceEnabled",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "AccountExplorerTemplate",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ManagedAccounts");

            migrationBuilder.RenameColumn(
                name: "TransactionExplorerTemplate",
                table: "Networks",
                newName: "HTLCTokenContractAddress");

            migrationBuilder.RenameColumn(
                name: "Logo",
                table: "Networks",
                newName: "HTLCNativeContractAddress");

            migrationBuilder.RenameColumn(
                name: "NetworkId",
                table: "ManagedAccounts",
                newName: "NetworkType");

            migrationBuilder.AlterColumn<string>(
                name: "FeeAmount",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "SourceAmount",
                table: "Swaps",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "FeeAmount",
                table: "Swaps",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "DestinationAmount",
                table: "Swaps",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "NativeTokenId",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string[]>(
                name: "LastFeeValues",
                table: "Expenses",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(decimal[]),
                oldType: "numeric[]");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAccounts_Address_NetworkType",
                table: "ManagedAccounts",
                columns: new[] { "Address", "NetworkType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManagedAccounts_Address_NetworkType",
                table: "ManagedAccounts");

            migrationBuilder.RenameColumn(
                name: "HTLCTokenContractAddress",
                table: "Networks",
                newName: "TransactionExplorerTemplate");

            migrationBuilder.RenameColumn(
                name: "HTLCNativeContractAddress",
                table: "Networks",
                newName: "Logo");

            migrationBuilder.RenameColumn(
                name: "NetworkType",
                table: "ManagedAccounts",
                newName: "NetworkId");

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeAmount",
                table: "Transactions",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsNative",
                table: "Tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Tokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Precision",
                table: "Tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "SourceAmount",
                table: "Swaps",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeAmount",
                table: "Swaps",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "DestinationAmount",
                table: "Swaps",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<double>(
                name: "Priority",
                table: "Nodes",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "TraceEnabled",
                table: "Nodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "NativeTokenId",
                table: "Networks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AccountExplorerTemplate",
                table: "Networks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ManagedAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Primary=0,Secondary=1,Rebalance=2");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "LastFeeValues",
                table: "Expenses",
                type: "numeric[]",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]");

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NetworkId = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,EvmMultiCallContract=3"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAccounts_Address",
                table: "ManagedAccounts",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAccounts_NetworkId",
                table: "ManagedAccounts",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_NetworkId",
                table: "Contracts",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagedAccounts_Networks_NetworkId",
                table: "ManagedAccounts",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
