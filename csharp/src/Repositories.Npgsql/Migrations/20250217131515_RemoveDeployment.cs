using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Train.Solver.Data.EF.Migrations;

/// <inheritdoc />
public partial class RemoveDeployment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Deployments");

        migrationBuilder.DropTable(
            name: "Apps");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Apps",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApiKey = table.Column<string>(type: "text", nullable: true),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                Name = table.Column<string>(type: "text", nullable: false),
                SandboxApiKey = table.Column<string>(type: "text", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Apps", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Deployments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                AppId = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                NetworkName = table.Column<string>(type: "text", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Deployments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Deployments_Apps_AppId",
                    column: x => x.AppId,
                    principalTable: "Apps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Apps_ApiKey",
            table: "Apps",
            column: "ApiKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Apps_SandboxApiKey",
            table: "Apps",
            column: "SandboxApiKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Deployments_AppId",
            table: "Deployments",
            column: "AppId");
    }
}
