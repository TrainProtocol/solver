using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TokenGroupId",
                table: "Tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Asset = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_TokenGroupId",
                table: "Tokens",
                column: "TokenGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_TokenGroups_TokenGroupId",
                table: "Tokens",
                column: "TokenGroupId",
                principalTable: "TokenGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_TokenGroups_TokenGroupId",
                table: "Tokens");

            migrationBuilder.DropTable(
                name: "TokenGroups");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_TokenGroupId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "TokenGroupId",
                table: "Tokens");
        }
    }
}
