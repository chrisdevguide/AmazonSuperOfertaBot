using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElAhorrador.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmazonProducts",
                columns: table => new
                {
                    Asin = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousPrices = table.Column<List<decimal>>(type: "numeric[]", nullable: true),
                    ReviewsCount = table.Column<int>(type: "integer", nullable: false),
                    Stars = table.Column<decimal>(type: "numeric", nullable: false),
                    HasStock = table.Column<bool>(type: "boolean", nullable: false),
                    InitialScrapedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastScrapedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmazonProducts", x => x.Asin);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Name);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmazonProducts");

            migrationBuilder.DropTable(
                name: "Configurations");
        }
    }
}
