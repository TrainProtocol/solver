using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Train.Solver.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RateProviders",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 100000, "SameAsset" },
                    { 100001, "Binance" }
                });

            migrationBuilder.InsertData(
                table: "ServiceFees",
                columns: new[] { "Id", "FeeInUsd", "FeePercentage", "Name" },
                values: new object[,]
                {
                    { 100000, 0m, 0m, "Free" },
                    { 100001, 0m, 0m, "Default" }
                });

            migrationBuilder.InsertData(
                table: "TokenPrices",
                columns: new[] { "Id", "CreatedDate", "ExternalId", "LastUpdated", "PriceInUsd", "Symbol" },
                values: new object[,]
                {
                    { 10002, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "bitcoin", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "BTC" },
                    { 10008, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fuel-network", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "FUEL" },
                    { 10014, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "avalanche-2", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "AVAX" },
                    { 10018, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "optimism", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "OP" },
                    { 10026, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ethereum", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "ETH" },
                    { 10035, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "solana", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "SOL" },
                    { 10043, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "dai", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "DAI" },
                    { 10046, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "usd-coin", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "USDC" },
                    { 10050, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "immutable-x", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "IMX" },
                    { 10054, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "binancecoin", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "BNB" },
                    { 10055, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "tether", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "USDT" },
                    { 10056, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "matic-network", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "MATIC" },
                    { 10063, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "polygon-ecosystem-token", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "POL" },
                    { 10069, new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ronin", new DateTimeOffset(new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0m, "RON" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RateProviders",
                keyColumn: "Id",
                keyValue: 100000);

            migrationBuilder.DeleteData(
                table: "RateProviders",
                keyColumn: "Id",
                keyValue: 100001);

            migrationBuilder.DeleteData(
                table: "ServiceFees",
                keyColumn: "Id",
                keyValue: 100000);

            migrationBuilder.DeleteData(
                table: "ServiceFees",
                keyColumn: "Id",
                keyValue: 100001);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10002);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10008);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10014);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10018);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10026);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10035);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10043);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10046);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10050);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10054);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10055);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10056);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10063);

            migrationBuilder.DeleteData(
                table: "TokenPrices",
                keyColumn: "Id",
                keyValue: 10069);
        }
    }
}
