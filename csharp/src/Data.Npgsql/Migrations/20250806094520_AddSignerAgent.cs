using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddSignerAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SignerAgentId",
                table: "Wallets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SignerAgents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SupportedTypes = table.Column<int[]>(type: "integer[]", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignerAgents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_SignerAgentId",
                table: "Wallets",
                column: "SignerAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_SignerAgents_SignerAgentId",
                table: "Wallets",
                column: "SignerAgentId",
                principalTable: "SignerAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_SignerAgents_SignerAgentId",
                table: "Wallets");

            migrationBuilder.DropTable(
                name: "SignerAgents");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_SignerAgentId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "SignerAgentId",
                table: "Wallets");
        }
    }
}
