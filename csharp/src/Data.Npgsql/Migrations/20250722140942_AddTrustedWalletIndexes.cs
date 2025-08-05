using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustedWalletIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TrustedWallets_Address_NetworkType",
                table: "TrustedWallets",
                columns: new[] { "Address", "NetworkType" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustedWallets_Name",
                table: "TrustedWallets",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrustedWallets_Address_NetworkType",
                table: "TrustedWallets");

            migrationBuilder.DropIndex(
                name: "IX_TrustedWallets_Name",
                table: "TrustedWallets");
        }
    }
}
