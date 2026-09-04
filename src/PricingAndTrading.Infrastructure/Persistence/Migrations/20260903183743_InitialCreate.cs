using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricingAndTrading.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatestPrices",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BidPrice = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    AskPrice = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    CurrentMarketPrice = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Spread = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    SpreadPercent = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestPrices", x => x.Symbol);
                });

            migrationBuilder.CreateTable(
                name: "OrderIdRegistrations",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderIdRegistrations", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    PersistenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Side = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "TradingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MaximumNotionalAmount = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    MaximumQuantity = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    MaximumPriceDeviationPercent = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    DuplicateOrderIdCheckEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SymbolWhitelistEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SymbolWhitelist = table.Column<string[]>(type: "text[]", nullable: false),
                    AutoTradingSpreadThresholdPercent = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderRejectionReasons",
                columns: table => new
                {
                    OrderPersistenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRejectionReasons", x => new { x.OrderPersistenceId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_OrderRejectionReasons_Orders_OrderPersistenceId",
                        column: x => x.OrderPersistenceId,
                        principalTable: "Orders",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderId",
                table: "Orders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Symbol_CreatedAt",
                table: "Orders",
                columns: new[] { "Symbol", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatestPrices");

            migrationBuilder.DropTable(
                name: "OrderIdRegistrations");

            migrationBuilder.DropTable(
                name: "OrderRejectionReasons");

            migrationBuilder.DropTable(
                name: "TradingRules");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
