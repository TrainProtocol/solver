using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNodeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nodes_Type_NetworkId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Nodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Nodes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Primary=0,DepositTracking=1,Public=2,Secondary=3");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_Type_NetworkId",
                table: "Nodes",
                columns: new[] { "Type", "NetworkId" },
                unique: true);
        }
    }
}
