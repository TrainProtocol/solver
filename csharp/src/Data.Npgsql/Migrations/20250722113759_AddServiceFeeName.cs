using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceFeeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Wallets");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ServiceFees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Name",
                table: "Wallets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFees_Name",
                table: "ServiceFees",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_Name",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_ServiceFees_Name",
                table: "ServiceFees");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ServiceFees");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Wallets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
