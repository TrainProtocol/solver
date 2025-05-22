using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddRebalanceAccountTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ManagedAccounts",
                type: "integer",
                nullable: false,
                comment: "Primary=0,Secondary=1,Rebalance=2",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Primary=0,Secondary=1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ManagedAccounts",
                type: "integer",
                nullable: false,
                comment: "Primary=0,Secondary=1",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Primary=0,Secondary=1,Rebalance=2");
        }
    }
}
