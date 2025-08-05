using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class ChangeWalletUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_Name",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_TrustedWallets_Name",
                table: "TrustedWallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Name_NetworkType",
                table: "Wallets",
                columns: new[] { "Name", "NetworkType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustedWallets_Name_NetworkType",
                table: "TrustedWallets",
                columns: new[] { "Name", "NetworkType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_Name_NetworkType",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_TrustedWallets_Name_NetworkType",
                table: "TrustedWallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Name",
                table: "Wallets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustedWallets_Name",
                table: "TrustedWallets",
                column: "Name",
                unique: true);
        }
    }
}
