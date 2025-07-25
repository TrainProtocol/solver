using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeProviderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "Nodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_ProviderName_NetworkId",
                table: "Nodes",
                columns: new[] { "ProviderName", "NetworkId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nodes_ProviderName_NetworkId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "Nodes");
        }
    }
}
