using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetricId",
                table: "Swaps",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SwapMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceNetwork = table.Column<string>(type: "text", nullable: false),
                    SourceToken = table.Column<string>(type: "text", nullable: false),
                    DestinationNetwork = table.Column<string>(type: "text", nullable: false),
                    DestinationToken = table.Column<string>(type: "text", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false),
                    VolumeInUsd = table.Column<decimal>(type: "numeric", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    ProfitInUsd = table.Column<decimal>(type: "numeric", nullable: false),
                    SwapId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwapMetrics_Swaps_SwapId",
                        column: x => x.SwapId,
                        principalTable: "Swaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrustedWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    NetworkType = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedWallets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_CreatedDate",
                table: "SwapMetrics",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_DestinationNetwork",
                table: "SwapMetrics",
                column: "DestinationNetwork");

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_SourceNetwork",
                table: "SwapMetrics",
                column: "SourceNetwork");

            migrationBuilder.CreateIndex(
                name: "IX_SwapMetrics_SwapId",
                table: "SwapMetrics",
                column: "SwapId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SwapMetrics");

            migrationBuilder.DropTable(
                name: "TrustedWallets");

            migrationBuilder.DropColumn(
                name: "MetricId",
                table: "Swaps");
        }
    }
}
