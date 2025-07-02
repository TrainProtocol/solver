using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class MakeNativeTokenNUllable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "NativeTokenId",
                table: "Networks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "NativeTokenId",
                table: "Networks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
