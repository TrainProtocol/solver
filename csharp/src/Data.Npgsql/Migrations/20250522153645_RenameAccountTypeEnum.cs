using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ManagedAccounts",
                type: "integer",
                nullable: false,
                comment: "Primary=0,Secondary=1",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "LP=0,Charging=1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ManagedAccounts",
                type: "integer",
                nullable: false,
                comment: "LP=0,Charging=1",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Primary=0,Secondary=1");
        }
    }
}
