using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Tokens_FeeTokenId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Tokens_TokenId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FeeTokenId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionHash_NetworkName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Confirmations",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FeeTokenId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NetworkName",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "Transactions",
                newName: "NetworkId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_TokenId",
                table: "Transactions",
                newName: "IX_Transactions_NetworkId");

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreExpenseFee",
                table: "Routes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionHash_NetworkId",
                table: "Transactions",
                columns: new[] { "TransactionHash", "NetworkId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Networks_NetworkId",
                table: "Transactions",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Networks_NetworkId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionHash_NetworkId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IgnoreExpenseFee",
                table: "Routes");

            migrationBuilder.RenameColumn(
                name: "NetworkId",
                table: "Transactions",
                newName: "TokenId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_NetworkId",
                table: "Transactions",
                newName: "IX_Transactions_TokenId");

            migrationBuilder.AddColumn<int>(
                name: "Confirmations",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FeeTokenId",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NetworkName",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FeeTokenId",
                table: "Transactions",
                column: "FeeTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionHash_NetworkName",
                table: "Transactions",
                columns: new[] { "TransactionHash", "NetworkName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Tokens_FeeTokenId",
                table: "Transactions",
                column: "FeeTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Tokens_TokenId",
                table: "Transactions",
                column: "TokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
