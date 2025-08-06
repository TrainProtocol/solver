using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddSignerAgentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SignerAgents_Name",
                table: "SignerAgents",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignerAgents_Name",
                table: "SignerAgents");
        }
    }
}
