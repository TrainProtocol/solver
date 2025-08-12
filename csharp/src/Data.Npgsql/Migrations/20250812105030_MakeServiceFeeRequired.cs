using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class MakeServiceFeeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_ServiceFees_ServiceFeeId",
                table: "Routes");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceFeeId",
                table: "Routes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_ServiceFees_ServiceFeeId",
                table: "Routes",
                column: "ServiceFeeId",
                principalTable: "ServiceFees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_ServiceFees_ServiceFeeId",
                table: "Routes");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceFeeId",
                table: "Routes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_ServiceFees_ServiceFeeId",
                table: "Routes",
                column: "ServiceFeeId",
                principalTable: "ServiceFees",
                principalColumn: "Id");
        }
    }
}
